using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services.Ticketing;

public class WaitlistService(
    IGenericRepository<Waitlist> waitlistRepository,
    IGenericRepository<TicketType> ticketTypeRepository,
    IGenericRepository<Event> eventRepository,
    IGenericRepository<AttendeeProfile> attendeeRepository,
    IGenericRepository<Ticket> ticketRepository,
    IGenericRepository<Payment> paymentRepository,
    IGenericRepository<WalletTransaction> walletTransactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMapper mapper) : IWaitlistService
{
    public async Task<WaitlistDto> JoinAsync(int ticketTypeId, CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticketTypeId}) was not found.");

        if (ticketType.RemainingQuantity > 0)
            throw new InvalidOperationException("Tickets are still available for this type. Purchase instead of joining the waitlist.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        if (eventEntity.Status != EventStatus.Published)
            throw new InvalidOperationException("Waitlist is only available for published events.");

        var now = DateTime.UtcNow;
        if (now >= eventEntity.StartDate)
            throw new InvalidOperationException("Waitlist is not available after the event has started.");

        if (now < ticketType.SaleStartDate || now > ticketType.SaleEndDate)
            throw new InvalidOperationException("This ticket type is not currently on sale.");

        if (await waitlistRepository.AnyAsync(
                w => w.TicketTypeId == ticketTypeId
                     && w.AttendeeId == attendeeId
                     && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified),
                cancellationToken))
            throw new InvalidOperationException("You are already on the waitlist for this ticket type.");

        var entry = new Waitlist
        {
            EventId = ticketType.EventId,
            TicketTypeId = ticketTypeId,
            AttendeeId = attendeeId,
            Status = WaitlistStatus.Waiting,
            RequestedAt = now
        };

        await waitlistRepository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<WaitlistDto>(entry);
    }

    public async Task<TicketDto> ConvertAsync(int waitlistId, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var attendeeId = currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required.");

            var entry = await waitlistRepository.GetByIdAsync(waitlistId, ct)
                ?? throw new KeyNotFoundException($"Waitlist ({waitlistId}) was not found.");

            if (entry.AttendeeId != attendeeId)
                throw new KeyNotFoundException($"Waitlist ({waitlistId}) was not found.");

            if (entry.Status != WaitlistStatus.Notified)
                throw new InvalidOperationException("Only notified waitlist entries can be converted.");

            var now = DateTime.UtcNow;
            if (entry.ExpiresAt is not null && entry.ExpiresAt < now)
            {
                entry.Status = WaitlistStatus.Expired;
                waitlistRepository.Update(entry);

                var heldType = await ticketTypeRepository.GetByIdAsync(entry.TicketTypeId, ct);
                if (heldType is not null)
                {
                    heldType.RemainingQuantity++;
                    ticketTypeRepository.Update(heldType);
                }

                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("This waitlist offer has expired.");
            }

            var ticketType = await ticketTypeRepository.GetByIdAsync(entry.TicketTypeId, ct)
                ?? throw new KeyNotFoundException($"TicketType ({entry.TicketTypeId}) was not found.");

            var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, ct)
                ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

            if (eventEntity.Status != EventStatus.Published)
                throw new InvalidOperationException("Tickets can only be purchased for published events.");

            if (now >= eventEntity.StartDate)
                throw new InvalidOperationException("Tickets cannot be purchased after the event has started.");

            if (now < ticketType.SaleStartDate || now > ticketType.SaleEndDate)
                throw new InvalidOperationException("This ticket type is not currently on sale.");

            var attendee = await attendeeRepository.GetByIdAsync(entry.AttendeeId, ct)
                ?? throw new KeyNotFoundException($"AttendeeProfile ({entry.AttendeeId}) was not found.");

            await TicketPurchaseGuard.EnsureUnderPerUserLimitAsync(
                ticketRepository,
                ticketType.Id,
                entry.AttendeeId,
                ticketType.MaxTicketsPerUser,
                ct);

            WalletBalanceGuard.Debit(attendee, ticketType.Price);
            entry.Status = WaitlistStatus.Converted;

            var ticket = new Ticket
            {
                TicketTypeId = ticketType.Id,
                AttendeeId = entry.AttendeeId,
                UniqueCode = Guid.NewGuid().ToString("N"),
                UnitPrice = ticketType.Price,
                Status = TicketStatus.Paid,
                PurchasedAt = now
            };

            await ticketRepository.AddAsync(ticket, ct);
            attendeeRepository.Update(attendee);
            waitlistRepository.Update(entry);
            await unitOfWork.SaveChangesAsync(ct);

            var payment = new Payment
            {
                TicketId = ticket.Id,
                AttendeeId = entry.AttendeeId,
                Amount = ticketType.Price,
                Status = PaymentStatus.Completed,
                TransactionCode = Guid.NewGuid().ToString("N")
            };

            await paymentRepository.AddAsync(payment, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await walletTransactionRepository.AddAsync(new WalletTransaction
            {
                AttendeeId = entry.AttendeeId,
                Amount = ticketType.Price,
                BalanceAfter = attendee.WalletBalance,
                Type = WalletTransactionType.Purchase,
                PaymentId = payment.Id,
                TicketId = ticket.Id
            }, ct);

            await unitOfWork.SaveChangesAsync(ct);

            return mapper.Map<TicketDto>(ticket);
        }, cancellationToken);
    }

    public async Task<WaitlistDto> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default)
    {
        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        EventOwnership.EnsureOwnedBy(eventEntity, currentUser.UserId);

        var entries = await waitlistRepository.FindAsync(w => w.TicketTypeId == ticketTypeId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var expired in entries.Where(e =>
                     e.Status == WaitlistStatus.Notified && e.ExpiresAt is not null && e.ExpiresAt < now))
        {
            expired.Status = WaitlistStatus.Expired;
            ticketType.RemainingQuantity++;
            waitlistRepository.Update(expired);
        }

        if (entries.Any(e => e.Status == WaitlistStatus.Notified && (e.ExpiresAt is null || e.ExpiresAt >= now)))
            throw new InvalidOperationException("Another attendee already has an active waitlist offer.");

        if (ticketType.RemainingQuantity <= 0)
            throw new InvalidOperationException("There are no available tickets to offer.");

        var next = entries
            .Where(e => e.Status == WaitlistStatus.Waiting)
            .OrderBy(e => e.RequestedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("There is nobody waiting for this ticket type.");

        ticketType.RemainingQuantity--;
        next.Status = WaitlistStatus.Notified;
        next.NotifiedAt = now;
        next.ExpiresAt = now.AddMinutes(30);

        ticketTypeRepository.Update(ticketType);
        waitlistRepository.Update(next);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<WaitlistDto>(next);
    }

    public async Task<PagedResult<WaitlistDto>> GetMyWaitlistAsync(
        WaitlistListQuery query,
        CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var entries = await waitlistRepository.FindAsync(w => w.AttendeeId == attendeeId, cancellationToken);
        if (query.Status is { } status)
            entries = entries.Where(e => e.Status == status).ToList();

        var ordered = entries.OrderByDescending(w => w.RequestedAt).ToList();
        return new PagedResult<WaitlistDto>
        {
            Items = ordered.Skip(query.Skip).Take(query.Take).Select(e => mapper.Map<WaitlistDto>(e)).ToList(),
            Page = Math.Max(query.Page, 1),
            PageSize = query.Take,
            TotalCount = ordered.Count
        };
    }
}

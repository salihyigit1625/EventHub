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

        if (DateTime.UtcNow >= eventEntity.StartDate)
            throw new InvalidOperationException("Waitlist is not available after the event has started.");

        if (await waitlistRepository.AnyAsync(
                w => w.TicketTypeId == ticketTypeId
                     && w.AttendeeId == attendeeId
                     && w.Status == WaitlistStatus.Waiting,
                cancellationToken))
            throw new InvalidOperationException("You are already on the waitlist for this ticket type.");

        var entry = new Waitlist
        {
            EventId = ticketType.EventId,
            TicketTypeId = ticketTypeId,
            AttendeeId = attendeeId,
            Status = WaitlistStatus.Waiting,
            RequestedAt = DateTime.UtcNow
        };

        await waitlistRepository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<WaitlistDto>(entry);
    }

    public async Task<TicketDto> ConvertAsync(int waitlistId, CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var entry = await waitlistRepository.GetByIdAsync(waitlistId, cancellationToken)
            ?? throw new KeyNotFoundException($"Waitlist ({waitlistId}) was not found.");

        if (entry.AttendeeId != attendeeId)
            throw new KeyNotFoundException($"Waitlist ({waitlistId}) was not found.");

        if (entry.Status != WaitlistStatus.Notified)
            throw new InvalidOperationException("Only notified waitlist entries can be converted.");

        if (entry.ExpiresAt is not null && entry.ExpiresAt < DateTime.UtcNow)
        {
            entry.Status = WaitlistStatus.Expired;
            waitlistRepository.Update(entry);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This waitlist offer has expired.");
        }

        var ticketType = await ticketTypeRepository.GetByIdAsync(entry.TicketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({entry.TicketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        if (eventEntity.Status != EventStatus.Published)
            throw new InvalidOperationException("Tickets can only be purchased for published events.");

        if (DateTime.UtcNow >= eventEntity.StartDate)
            throw new InvalidOperationException("Tickets cannot be purchased after the event has started.");

        if (ticketType.RemainingQuantity <= 0)
            throw new InvalidOperationException("This ticket type is still sold out.");

        var attendee = await attendeeRepository.GetByIdAsync(entry.AttendeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"AttendeeProfile ({entry.AttendeeId}) was not found.");

        if (attendee.WalletBalance < ticketType.Price)
            throw new InvalidOperationException("Insufficient wallet balance.");

        var now = DateTime.UtcNow;
        ticketType.RemainingQuantity--;
        attendee.WalletBalance -= ticketType.Price;
        attendee.UpdatedAt = now;
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

        await ticketRepository.AddAsync(ticket, cancellationToken);
        ticketTypeRepository.Update(ticketType);
        attendeeRepository.Update(attendee);
        waitlistRepository.Update(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var payment = new Payment
        {
            TicketId = ticket.Id,
            AttendeeId = entry.AttendeeId,
            Amount = ticketType.Price,
            Status = PaymentStatus.Completed,
            TransactionCode = Guid.NewGuid().ToString("N")
        };

        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await walletTransactionRepository.AddAsync(new WalletTransaction
        {
            AttendeeId = entry.AttendeeId,
            Amount = ticketType.Price,
            BalanceAfter = attendee.WalletBalance,
            Type = WalletTransactionType.Purchase,
            PaymentId = payment.Id,
            TicketId = ticket.Id
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TicketDto>(ticket);
    }

    public async Task<WaitlistDto> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default)
    {
        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        EventOwnership.EnsureOwnedBy(eventEntity, currentUser.UserId);

        if (ticketType.RemainingQuantity <= 0)
            throw new InvalidOperationException("There are no available tickets to offer.");

        var entries = await waitlistRepository.FindAsync(w => w.TicketTypeId == ticketTypeId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var expired in entries.Where(e => e.Status == WaitlistStatus.Notified && e.ExpiresAt < now))
        {
            expired.Status = WaitlistStatus.Expired;
            waitlistRepository.Update(expired);
        }

        if (entries.Any(e => e.Status == WaitlistStatus.Notified && (e.ExpiresAt is null || e.ExpiresAt >= now)))
            throw new InvalidOperationException("Another attendee already has an active waitlist offer.");

        var next = entries
            .Where(e => e.Status == WaitlistStatus.Waiting)
            .OrderBy(e => e.RequestedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("There is nobody waiting for this ticket type.");

        next.Status = WaitlistStatus.Notified;
        next.NotifiedAt = now;
        next.ExpiresAt = now.AddMinutes(30);
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

using AutoMapper;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
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
        var entry = await waitlistRepository.GetByIdAsync(waitlistId, cancellationToken)
            ?? throw new KeyNotFoundException($"Waitlist ({waitlistId}) was not found.");

        if (entry.Status is not (WaitlistStatus.Waiting or WaitlistStatus.Notified))
            throw new InvalidOperationException("This waitlist entry cannot be converted.");

        if (entry.ExpiresAt is not null && entry.ExpiresAt < DateTime.UtcNow)
        {
            entry.Status = WaitlistStatus.Expired;
            waitlistRepository.Update(entry);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This waitlist offer has expired.");
        }

        var ticketType = await ticketTypeRepository.GetByIdAsync(entry.TicketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({entry.TicketTypeId}) was not found.");

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

    public async Task<IReadOnlyList<WaitlistDto>> GetMyWaitlistAsync(CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var entries = await waitlistRepository.FindAsync(w => w.AttendeeId == attendeeId, cancellationToken);
        return entries.OrderByDescending(w => w.RequestedAt).Select(e => mapper.Map<WaitlistDto>(e)).ToList();
    }
}

using AutoMapper;
using EventHub.Application.Common;
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

public class TicketService(
    IGenericRepository<Ticket> ticketRepository,
    IGenericRepository<TicketType> ticketTypeRepository,
    IGenericRepository<Event> eventRepository,
    IGenericRepository<AttendeeProfile> attendeeRepository,
    IGenericRepository<Payment> paymentRepository,
    IGenericRepository<WalletTransaction> walletTransactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMapper mapper) : ITicketService
{
    public async Task<TicketDto> PurchaseAsync(int ticketTypeId, CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        if (eventEntity.Status != EventStatus.Published)
            throw new InvalidOperationException("Tickets can only be purchased for published events.");

        var now = DateTime.UtcNow;
        if (now < ticketType.SaleStartDate || now > ticketType.SaleEndDate)
            throw new InvalidOperationException("This ticket type is not currently on sale.");

        if (ticketType.RemainingQuantity <= 0)
            throw new InvalidOperationException("This ticket type is sold out.");

        var attendee = await attendeeRepository.GetByIdAsync(attendeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"AttendeeProfile ({attendeeId}) was not found.");

        if (attendee.WalletBalance < ticketType.Price)
            throw new InvalidOperationException("Insufficient wallet balance.");

        ticketType.RemainingQuantity--;
        attendee.WalletBalance -= ticketType.Price;
        attendee.UpdatedAt = now;

        var ticket = new Ticket
        {
            TicketTypeId = ticketType.Id,
            AttendeeId = attendeeId,
            UniqueCode = Guid.NewGuid().ToString("N"),
            UnitPrice = ticketType.Price,
            Status = TicketStatus.Paid,
            PurchasedAt = now
        };

        await ticketRepository.AddAsync(ticket, cancellationToken);
        ticketTypeRepository.Update(ticketType);
        attendeeRepository.Update(attendee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var payment = new Payment
        {
            TicketId = ticket.Id,
            AttendeeId = attendeeId,
            Amount = ticketType.Price,
            Status = PaymentStatus.Completed,
            TransactionCode = Guid.NewGuid().ToString("N")
        };

        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await walletTransactionRepository.AddAsync(new WalletTransaction
        {
            AttendeeId = attendeeId,
            Amount = ticketType.Price,
            BalanceAfter = attendee.WalletBalance,
            Type = WalletTransactionType.Purchase,
            PaymentId = payment.Id,
            TicketId = ticket.Id
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TicketDto>(ticket);
    }

    public async Task<TicketDto> CancelAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket ({ticketId}) was not found.");

        if (ticket.Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be cancelled.");

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticket.TicketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        var deadline = eventEntity.StartDate.AddHours(-eventEntity.CancellationDeadlineHours);
        if (DateTime.UtcNow > deadline)
            throw new InvalidOperationException("The cancellation deadline for this event has passed.");

        var attendee = await attendeeRepository.GetByIdAsync(ticket.AttendeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"AttendeeProfile ({ticket.AttendeeId}) was not found.");

        ticketType.RemainingQuantity++;
        ticket.Status = TicketStatus.Cancelled;
        attendee.WalletBalance += ticket.UnitPrice;
        attendee.UpdatedAt = DateTime.UtcNow;

        var payment = await paymentRepository.FirstOrDefaultAsync(p => p.TicketId == ticket.Id, cancellationToken);
        if (payment is not null)
        {
            payment.Status = PaymentStatus.Refunded;
            paymentRepository.Update(payment);
        }

        await walletTransactionRepository.AddAsync(new WalletTransaction
        {
            AttendeeId = ticket.AttendeeId,
            Amount = ticket.UnitPrice,
            BalanceAfter = attendee.WalletBalance,
            Type = WalletTransactionType.Refund,
            PaymentId = payment?.Id,
            TicketId = ticket.Id
        }, cancellationToken);

        ticketRepository.Update(ticket);
        ticketTypeRepository.Update(ticketType);
        attendeeRepository.Update(attendee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TicketDto>(ticket);
    }

    public async Task<PagedResult<TicketDto>> GetMyTicketsAsync(
        TicketListQuery query,
        CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var tickets = await ticketRepository.FindAsync(t => t.AttendeeId == attendeeId, cancellationToken);
        if (query.Status is { } status)
            tickets = tickets.Where(t => t.Status == status).ToList();

        var ordered = tickets.OrderByDescending(t => t.CreatedAt).ToList();
        return new PagedResult<TicketDto>
        {
            Items = ordered.Skip(query.Skip).Take(query.Take).Select(t => mapper.Map<TicketDto>(t)).ToList(),
            Page = Math.Max(query.Page, 1),
            PageSize = query.Take,
            TotalCount = ordered.Count
        };
    }

    public async Task<TicketDto> GetByCodeAsync(string uniqueCode, CancellationToken cancellationToken = default)
    {
        var ticket = await ticketRepository.FirstOrDefaultAsync(t => t.UniqueCode == uniqueCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket ({uniqueCode}) was not found.");

        return mapper.Map<TicketDto>(ticket);
    }
}

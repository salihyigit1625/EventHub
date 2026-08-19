using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services.Profiles;

public class GateStaffService(
    IGenericRepository<Ticket> ticketRepository,
    IGenericRepository<CheckInLog> checkInLogRepository,
    IGenericRepository<GateStaffProfile> gateStaffRepository,
    IGenericRepository<User> userRepository,
    IGenericRepository<Event> eventRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IGateStaffService
{
    public async Task<CheckInResultDto> CheckInAsync(
        CheckInTicketDto dto,
        CancellationToken cancellationToken = default)
    {
        var gateStaffId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var scannedCode = dto.UniqueCode.Trim();
        var now = DateTime.UtcNow;

        var ticket = await ticketRepository.FirstOrDefaultAsync(t => t.UniqueCode == scannedCode, cancellationToken);
        var (isSuccessful, failureReason, ticketId) = Evaluate(ticket);

        if (isSuccessful && ticket is not null)
        {
            ticket.Status = TicketStatus.CheckedIn;
            ticket.CheckedInAt = now;
            ticketRepository.Update(ticket);
        }

        await checkInLogRepository.AddAsync(new CheckInLog
        {
            TicketId = ticketId,
            GateStaffId = gateStaffId,
            IsSuccessful = isSuccessful,
            FailureReason = failureReason,
            ScannedCode = scannedCode,
            DeviceLocation = dto.DeviceLocation,
            CheckedInAt = now
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CheckInResultDto
        {
            IsSuccessful = isSuccessful,
            ScannedCode = scannedCode,
            FailureReason = failureReason,
            TicketId = ticketId,
            CheckedInAt = now
        };
    }

    public async Task<GateStaffProfileDto> GetAssignedEventAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var profile = await gateStaffRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"GateStaffProfile ({userId}) was not found.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({userId}) was not found.");

        string? eventTitle = null;
        if (profile.AssignedEventId is { } eventId)
        {
            var assignedEvent = await eventRepository.GetByIdAsync(eventId, cancellationToken);
            eventTitle = assignedEvent?.Title;
        }

        return new GateStaffProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AssignedEventId = profile.AssignedEventId,
            AssignedEventTitle = eventTitle
        };
    }

    private static (bool IsSuccessful, string? FailureReason, int? TicketId) Evaluate(Ticket? ticket)
    {
        if (ticket is null)
            return (false, "Ticket was not found.", null);

        return ticket.Status switch
        {
            TicketStatus.Paid => (true, null, ticket.Id),
            TicketStatus.CheckedIn => (false, "Ticket has already been checked in.", ticket.Id),
            TicketStatus.Cancelled => (false, "Ticket has been cancelled.", ticket.Id),
            TicketStatus.Refunded => (false, "Ticket has been refunded.", ticket.Id),
            TicketStatus.Reserved => (false, "Ticket has not been paid.", ticket.Id),
            _ => (false, "Ticket is not valid for check-in.", ticket.Id)
        };
    }
}

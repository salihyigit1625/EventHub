using EventHub.Application.Interfaces.Persistence;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common;

public static class TicketPurchaseGuard
{
    public static async Task EnsureUnderPerUserLimitAsync(
        IGenericRepository<Ticket> ticketRepository,
        int ticketTypeId,
        int attendeeId,
        int maxTicketsPerUser,
        CancellationToken cancellationToken = default)
    {
        var owned = await ticketRepository.FindAsync(
            t => t.TicketTypeId == ticketTypeId
                 && t.AttendeeId == attendeeId
                 && (t.Status == TicketStatus.Paid || t.Status == TicketStatus.CheckedIn),
            cancellationToken);

        if (owned.Count >= maxTicketsPerUser)
            throw new InvalidOperationException(
                $"You have reached the purchase limit ({maxTicketsPerUser}) for this ticket type.");
    }
}

using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common;

/// <summary>
/// Conditional Paid → CheckedIn transition for concurrent gate scans.
/// </summary>
public static class TicketCheckInGuard
{
    public static bool TryClaimForCheckIn(Ticket ticket, DateTime checkedInAt)
    {
        if (ticket.Status != TicketStatus.Paid)
            return false;

        ticket.Status = TicketStatus.CheckedIn;
        ticket.CheckedInAt = checkedInAt;
        return true;
    }
}

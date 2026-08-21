using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common;

/// <summary>
/// Idempotent waitlist hold expiry: Notified + past ExpiresAt → Expired and release one seat.
/// </summary>
public static class WaitlistHoldGuard
{
    public static bool TryExpireHold(Waitlist entry, TicketType? ticketType, DateTime utcNow)
    {
        if (entry.Status != WaitlistStatus.Notified)
            return false;

        if (entry.ExpiresAt is null || entry.ExpiresAt >= utcNow)
            return false;

        entry.Status = WaitlistStatus.Expired;

        if (ticketType is not null)
            ticketType.RemainingQuantity++;

        return true;
    }
}

using EventHub.Application.Common;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Tests.Common;

[TestFixture]
public class TicketCheckInGuardTests
{
    [Test]
    public void TryClaimForCheckIn_OnlySucceedsOnce()
    {
        var ticket = new Ticket { Status = TicketStatus.Paid };
        var at = DateTime.UtcNow;

        Assert.That(TicketCheckInGuard.TryClaimForCheckIn(ticket, at), Is.True);
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.CheckedIn));
        Assert.That(ticket.CheckedInAt, Is.EqualTo(at));
        Assert.That(TicketCheckInGuard.TryClaimForCheckIn(ticket, at.AddSeconds(1)), Is.False);
    }

    [Test]
    public void TryClaimForCheckIn_NonPaid_ReturnsFalse()
    {
        var ticket = new Ticket { Status = TicketStatus.Reserved };
        Assert.That(TicketCheckInGuard.TryClaimForCheckIn(ticket, DateTime.UtcNow), Is.False);
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Reserved));
    }
}

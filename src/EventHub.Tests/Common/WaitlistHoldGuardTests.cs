using EventHub.Application.Common;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Tests.Common;

[TestFixture]
public class WaitlistHoldGuardTests
{
    [Test]
    public void TryExpireHold_NotifiedAndPastExpiry_ExpiresAndReleasesStock()
    {
        var type = new TicketType { RemainingQuantity = 0 };
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, type, DateTime.UtcNow), Is.True);
        Assert.That(entry.Status, Is.EqualTo(WaitlistStatus.Expired));
        Assert.That(type.RemainingQuantity, Is.EqualTo(1));
    }

    [Test]
    public void TryExpireHold_AlreadyExpired_ReturnsFalse()
    {
        var type = new TicketType { RemainingQuantity = 1 };
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Expired,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, type, DateTime.UtcNow), Is.False);
        Assert.That(type.RemainingQuantity, Is.EqualTo(1));
    }

    [Test]
    public void TryExpireHold_ActiveHold_ReturnsFalse()
    {
        var type = new TicketType { RemainingQuantity = 0 };
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, type, DateTime.UtcNow), Is.False);
        Assert.That(entry.Status, Is.EqualTo(WaitlistStatus.Notified));
        Assert.That(type.RemainingQuantity, Is.EqualTo(0));
    }

    [Test]
    public void TryExpireHold_NullTicketType_ExpiresWithoutStockBump()
    {
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, null, DateTime.UtcNow), Is.True);
        Assert.That(entry.Status, Is.EqualTo(WaitlistStatus.Expired));
    }

    [Test]
    public void TryExpireHold_WaitingStatus_ReturnsFalse()
    {
        var type = new TicketType { RemainingQuantity = 0 };
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Waiting,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, type, DateTime.UtcNow), Is.False);
        Assert.That(type.RemainingQuantity, Is.EqualTo(0));
    }

    [Test]
    public void TryExpireHold_NullExpiresAt_ReturnsFalse()
    {
        var type = new TicketType { RemainingQuantity = 0 };
        var entry = new Waitlist
        {
            Status = WaitlistStatus.Notified,
            ExpiresAt = null
        };

        Assert.That(WaitlistHoldGuard.TryExpireHold(entry, type, DateTime.UtcNow), Is.False);
        Assert.That(type.RemainingQuantity, Is.EqualTo(0));
    }
}

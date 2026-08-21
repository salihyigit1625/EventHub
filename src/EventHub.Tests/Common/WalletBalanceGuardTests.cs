using EventHub.Application.Common;
using EventHub.Domain.Entities.Profiles;

namespace EventHub.Tests.Common;

[TestFixture]
public class WalletBalanceGuardTests
{
    [Test]
    public void Debit_UpdatesBalanceAndUpdatedAt()
    {
        var attendee = new AttendeeProfile { WalletBalance = 100m };

        WalletBalanceGuard.Debit(attendee, 40m);

        Assert.That(attendee.WalletBalance, Is.EqualTo(60m));
        Assert.That(attendee.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void Debit_InsufficientBalance_Throws()
    {
        var attendee = new AttendeeProfile { WalletBalance = 10m };

        Assert.Throws<InvalidOperationException>(() => WalletBalanceGuard.Debit(attendee, 11m));
        Assert.That(attendee.WalletBalance, Is.EqualTo(10m));
    }

    [Test]
    public void Debit_NonPositiveAmount_Throws([Values(0, -1)] decimal amount)
    {
        var attendee = new AttendeeProfile { WalletBalance = 50m };
        Assert.Throws<InvalidOperationException>(() => WalletBalanceGuard.Debit(attendee, amount));
    }

    [Test]
    public void Credit_IncreasesBalance()
    {
        var attendee = new AttendeeProfile { WalletBalance = 20m };

        WalletBalanceGuard.Credit(attendee, 15m);

        Assert.That(attendee.WalletBalance, Is.EqualTo(35m));
        Assert.That(attendee.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void Credit_NonPositiveAmount_Throws([Values(0, -5)] decimal amount)
    {
        var attendee = new AttendeeProfile { WalletBalance = 20m };
        Assert.Throws<InvalidOperationException>(() => WalletBalanceGuard.Credit(attendee, amount));
    }
}

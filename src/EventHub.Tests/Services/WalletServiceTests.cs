using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class WalletServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Deposit_IncreasesBalanceAndWritesTransaction()
    {
        _db.SeedAttendee(20, 40m);
        _db.CurrentUser.UserId = 20;

        var result = await _db.CreateWalletService().DepositAsync(new DepositDto { Amount = 25.50m });

        Assert.That(result.Balance, Is.EqualTo(65.50m));
        Assert.That(_db.WalletTransactions.Items.Single().Type, Is.EqualTo(WalletTransactionType.Deposit));
        Assert.That(_db.WalletTransactions.Items.Single().BalanceAfter, Is.EqualTo(65.50m));
    }

    [Test]
    public void Deposit_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateWalletService().DepositAsync(new DepositDto { Amount = 10 }));
    }

    [Test]
    public void Deposit_MissingProfile_Throws()
    {
        _db.CurrentUser.UserId = 20;
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateWalletService().DepositAsync(new DepositDto { Amount = 10 }));
    }

    [Test]
    public async Task GetBalance_ReturnsCurrentAmount()
    {
        _db.SeedAttendee(20, 123m);
        _db.CurrentUser.UserId = 20;

        var balance = await _db.CreateWalletService().GetBalanceAsync();
        Assert.That(balance.Balance, Is.EqualTo(123m));
        Assert.That(balance.AttendeeId, Is.EqualTo(20));
    }

    [Test]
    public async Task GetTransactions_PagesNewestFirstForCurrentUser()
    {
        _db.CurrentUser.UserId = 20;
        _db.WalletTransactions.Seed(
            new WalletTransaction { AttendeeId = 20, Amount = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-3) },
            new WalletTransaction { AttendeeId = 20, Amount = 2, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new WalletTransaction { AttendeeId = 21, Amount = 99, CreatedAt = DateTime.UtcNow });

        var page = await _db.CreateWalletService().GetTransactionsAsync(new PagingQuery { Page = 1, PageSize = 1 });

        Assert.That(page.TotalCount, Is.EqualTo(2));
        Assert.That(page.Items, Has.Count.EqualTo(1));
        Assert.That(page.Items[0].Amount, Is.EqualTo(2m));
    }

    [Test]
    public void GetBalance_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateWalletService().GetBalanceAsync());
    }

    [Test]
    public void GetTransactions_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateWalletService().GetTransactionsAsync(new PagingQuery()));
    }
}

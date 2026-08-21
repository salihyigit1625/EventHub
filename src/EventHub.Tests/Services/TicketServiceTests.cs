using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class TicketServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Purchase_DecrementsInventoryAndWallet()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 3, price: 80m);
        _db.SeedAttendee(20, 200m);
        _db.CurrentUser.UserId = 20;

        var ticket = await _db.CreateTicketService().PurchaseAsync(type.Id);

        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
        Assert.That(ticket.AttendeeId, Is.EqualTo(20));
        Assert.That(type.RemainingQuantity, Is.EqualTo(2));
        Assert.That(_db.Attendees.Items.Single().WalletBalance, Is.EqualTo(120m));
        Assert.That(_db.Payments.Items.Single().Status, Is.EqualTo(PaymentStatus.Completed));
        Assert.That(_db.WalletTransactions.Items.Single().Type, Is.EqualTo(WalletTransactionType.Purchase));
        Assert.That(ticket.UniqueCode, Has.Length.EqualTo(32));
    }

    [Test]
    public void Purchase_UnpublishedEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        var type = _db.SeedTicketType(evt.Id);
        _db.SeedAttendee();
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Purchase_EventAlreadyStarted_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        _db.SeedAttendee();
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Purchase_OutsideSaleWindow_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, saleStart: DateTime.UtcNow.AddDays(1), saleEnd: DateTime.UtcNow.AddDays(2));
        _db.SeedAttendee();
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Purchase_SoldOut_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 10);
        _db.SeedAttendee();
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Purchase_InsufficientBalance_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, price: 100m);
        _db.SeedAttendee(20, 10m);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Purchase_AtPerUserLimit_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 5, maxTicketsPerUser: 1);
        _db.SeedAttendee(20, 500m);
        _db.SeedTicket(type.Id, 20);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().PurchaseAsync(type.Id));
        Assert.That(type.RemainingQuantity, Is.EqualTo(5));
    }

    [Test]
    public async Task Purchase_CancelledTicket_DoesNotCountTowardLimit()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 5, price: 50m, maxTicketsPerUser: 1);
        _db.SeedAttendee(20, 500m);
        _db.SeedTicket(type.Id, 20, TicketStatus.Cancelled);
        _db.CurrentUser.UserId = 20;

        var ticket = await _db.CreateTicketService().PurchaseAsync(type.Id);

        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
        Assert.That(type.RemainingQuantity, Is.EqualTo(4));
    }

    [Test]
    public void Purchase_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _db.CreateTicketService().PurchaseAsync(1));
    }

    [Test]
    public async Task Cancel_RestoresInventoryAndWallet()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10), cancellationDeadlineHours: 24);
        var type = _db.SeedTicketType(evt.Id, remaining: 4, total: 5);
        var attendee = _db.SeedAttendee(20, 0m);
        var ticket = _db.SeedTicket(type.Id, attendee.UserId, price: 100m);
        _db.Payments.Seed(new Payment
        {
            TicketId = ticket.Id,
            AttendeeId = attendee.UserId,
            Amount = 100m,
            Status = PaymentStatus.Completed
        });
        _db.CurrentUser.UserId = 20;

        var cancelled = await _db.CreateTicketService().CancelAsync(ticket.Id);

        Assert.That(cancelled.Status, Is.EqualTo(TicketStatus.Cancelled));
        Assert.That(type.RemainingQuantity, Is.EqualTo(5));
        Assert.That(attendee.WalletBalance, Is.EqualTo(100m));
        Assert.That(_db.Payments.Items.Single().Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public void Cancel_OtherUsersTicket_ThrowsNotFound()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10));
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 1);
        var owner = _db.SeedAttendee(20, 0m);
        var ticket = _db.SeedTicket(type.Id, owner.UserId);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateTicketService().CancelAsync(ticket.Id));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
        Assert.That(owner.WalletBalance, Is.EqualTo(0m));
        Assert.That(type.RemainingQuantity, Is.EqualTo(0));
    }

    [Test]
    public void Cancel_Unauthenticated_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10));
        var type = _db.SeedTicketType(evt.Id);
        var attendee = _db.SeedAttendee();
        var ticket = _db.SeedTicket(type.Id, attendee.UserId);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _db.CreateTicketService().CancelAsync(ticket.Id));
    }

    [Test]
    public void Cancel_AfterDeadline_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(1), cancellationDeadlineHours: 48);
        var type = _db.SeedTicketType(evt.Id);
        var attendee = _db.SeedAttendee();
        var ticket = _db.SeedTicket(type.Id, attendee.UserId);
        _db.CurrentUser.UserId = attendee.UserId;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().CancelAsync(ticket.Id));
    }

    [Test]
    public void Cancel_NonPaid_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10));
        var type = _db.SeedTicketType(evt.Id);
        var attendee = _db.SeedAttendee();
        var ticket = _db.SeedTicket(type.Id, attendee.UserId, TicketStatus.CheckedIn);
        _db.CurrentUser.UserId = attendee.UserId;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().CancelAsync(ticket.Id));
    }

    [Test]
    public void Cancel_CancelledEvent_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10), status: EventStatus.Cancelled);
        var type = _db.SeedTicketType(evt.Id);
        var attendee = _db.SeedAttendee();
        var ticket = _db.SeedTicket(type.Id, attendee.UserId);
        _db.CurrentUser.UserId = attendee.UserId;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateTicketService().CancelAsync(ticket.Id));
    }

    [Test]
    public async Task GetMyTickets_FiltersByAttendeeAndStatus()
    {
        _db.CurrentUser.UserId = 20;
        _db.Tickets.Seed(
            new Ticket { AttendeeId = 20, UniqueCode = "a", Status = TicketStatus.Paid, TicketTypeId = 1 },
            new Ticket { AttendeeId = 20, UniqueCode = "b", Status = TicketStatus.Cancelled, TicketTypeId = 1 },
            new Ticket { AttendeeId = 21, UniqueCode = "c", Status = TicketStatus.Paid, TicketTypeId = 1 });

        var page = await _db.CreateTicketService().GetMyTicketsAsync(new TicketListQuery
        {
            Status = TicketStatus.Paid
        });

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().UniqueCode, Is.EqualTo("a"));
    }

    [Test]
    public async Task GetByCode_OwnTicket_ReturnsTicket()
    {
        _db.SeedTicket(1, 20, code: "abc123");
        _db.CurrentUser.UserId = 20;

        var dto = await _db.CreateTicketService().GetByCodeAsync("abc123");
        Assert.That(dto.UniqueCode, Is.EqualTo("abc123"));
    }

    [Test]
    public void GetByCode_OtherUsersTicket_ThrowsNotFound()
    {
        _db.SeedTicket(1, 20, code: "abc123");
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateTicketService().GetByCodeAsync("abc123"));
    }

    [Test]
    public void GetByCode_Unknown_Throws()
    {
        _db.CurrentUser.UserId = 20;
        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateTicketService().GetByCodeAsync("missing"));
    }

    [Test]
    public void GetByCode_Unauthenticated_Throws()
    {
        _db.SeedTicket(1, 20, code: "abc123");
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _db.CreateTicketService().GetByCodeAsync("abc123"));
    }

    [Test]
    public void Purchase_CheckedInTicket_CountsTowardLimit()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 5, maxTicketsPerUser: 1);
        _db.SeedAttendee(20, 500m);
        _db.SeedTicket(type.Id, 20, TicketStatus.CheckedIn);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketService().PurchaseAsync(type.Id));
    }

    [Test]
    public void Cancel_CompletedEvent_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddDays(10), status: EventStatus.Completed);
        var type = _db.SeedTicketType(evt.Id, remaining: 4, total: 5);
        var ticket = _db.SeedTicket(type.Id, 20);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketService().CancelAsync(ticket.Id));
    }

    [Test]
    public async Task Purchase_CompletesInventoryWalletAndPayment()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 3, price: 10m);
        _db.SeedAttendee(20, 100m);
        _db.CurrentUser.UserId = 20;

        await _db.CreateTicketService().PurchaseAsync(type.Id);

        Assert.That(_db.Tickets.Items, Has.Count.EqualTo(1));
        Assert.That(_db.Payments.Items, Has.Count.EqualTo(1));
        Assert.That(_db.WalletTransactions.Items, Has.Count.EqualTo(1));
        Assert.That(type.RemainingQuantity, Is.EqualTo(2));
    }
}

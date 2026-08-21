using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class WaitlistServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Join_WhenSoldOut_CreatesWaitingEntry()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 10);
        _db.CurrentUser.UserId = 20;

        var entry = await _db.CreateWaitlistService().JoinAsync(type.Id);

        Assert.That(entry.Status, Is.EqualTo(WaitlistStatus.Waiting));
        Assert.That(entry.EventId, Is.EqualTo(evt.Id));
        Assert.That(entry.AttendeeId, Is.EqualTo(20));
    }

    [Test]
    public void Join_WhenTicketsRemain_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateWaitlistService().JoinAsync(type.Id));
    }

    [Test]
    public void Join_UnpublishedEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateWaitlistService().JoinAsync(type.Id));
    }

    [Test]
    public void Join_EventAlreadyStarted_Throws()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.CurrentUser.UserId = 20;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateWaitlistService().JoinAsync(type.Id));
    }

    [Test]
    public async Task Join_DuplicateWaiting_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.CurrentUser.UserId = 20;
        await _db.CreateWaitlistService().JoinAsync(type.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateWaitlistService().JoinAsync(type.Id));
    }

    [Test]
    public async Task NotifyNext_SelectsFifoAndSetsThirtyMinuteExpiry()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 10;
        _db.Waitlists.Seed(
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 21,
                Status = WaitlistStatus.Waiting,
                RequestedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 20,
                Status = WaitlistStatus.Waiting,
                RequestedAt = DateTime.UtcNow.AddMinutes(-30)
            });

        var notified = await _db.CreateWaitlistService().NotifyNextAsync(type.Id);

        Assert.That(notified.AttendeeId, Is.EqualTo(20));
        Assert.That(notified.Status, Is.EqualTo(WaitlistStatus.Notified));
        Assert.That(notified.ExpiresAt, Is.EqualTo(notified.NotifiedAt!.Value.AddMinutes(30)).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void NotifyNext_OtherOrganizersEvent_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateWaitlistService().NotifyNextAsync(type.Id));
    }

    [Test]
    public void NotifyNext_NoRemainingTickets_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().NotifyNextAsync(type.Id));
    }

    [Test]
    public void NotifyNext_ActiveOfferExists_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 10;
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 20,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().NotifyNextAsync(type.Id));
    }

    [Test]
    public async Task NotifyNext_ExpiresStaleOffersThenNotifiesWaiting()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 10;
        var stale = new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 21,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        var waiting = new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 20,
            Status = WaitlistStatus.Waiting,
            RequestedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _db.Waitlists.Seed(stale, waiting);

        var notified = await _db.CreateWaitlistService().NotifyNextAsync(type.Id);

        Assert.That(stale.Status, Is.EqualTo(WaitlistStatus.Expired));
        Assert.That(notified.AttendeeId, Is.EqualTo(20));
    }

    [Test]
    public void NotifyNext_NobodyWaiting_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        var type = _db.SeedTicketType(evt.Id, remaining: 1);
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().NotifyNextAsync(type.Id));
    }

    [Test]
    public void Convert_WaitingStatus_Throws()
    {
        _db.CurrentUser.UserId = 20;
        _db.Waitlists.Seed(new Waitlist { Status = WaitlistStatus.Waiting, TicketTypeId = 1, AttendeeId = 20 });
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id));
    }

    [Test]
    public void Convert_ExpiredOffer_MarksExpiredAndThrows()
    {
        _db.CurrentUser.UserId = 20;
        _db.Waitlists.Seed(new Waitlist
        {
            Status = WaitlistStatus.Notified,
            TicketTypeId = 1,
            AttendeeId = 20,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id));
        Assert.That(_db.Waitlists.Items[0].Status, Is.EqualTo(WaitlistStatus.Expired));
    }

    [Test]
    public async Task Convert_OwnNotified_PurchasesTicket()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 1, price: 50m);
        var attendee = _db.SeedAttendee(20, 80m);
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = attendee.UserId,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        _db.CurrentUser.UserId = 20;

        var ticket = await _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id);

        Assert.That(ticket.AttendeeId, Is.EqualTo(20));
        Assert.That(_db.Waitlists.Items[0].Status, Is.EqualTo(WaitlistStatus.Converted));
        Assert.That(type.RemainingQuantity, Is.EqualTo(0));
        Assert.That(attendee.WalletBalance, Is.EqualTo(30m));
    }

    [Test]
    public void Convert_OtherUsersEntry_ThrowsNotFound()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 1, price: 50m);
        var attendee = _db.SeedAttendee(20, 80m);
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = attendee.UserId,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id));
        Assert.That(_db.Waitlists.Items[0].Status, Is.EqualTo(WaitlistStatus.Notified));
        Assert.That(attendee.WalletBalance, Is.EqualTo(80m));
        Assert.That(type.RemainingQuantity, Is.EqualTo(1));
    }

    [Test]
    public void Convert_SoldOutAfterNotify_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.SeedAttendee(20, 500m);
        _db.CurrentUser.UserId = 20;
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 20,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id));
    }

    [Test]
    public void Convert_InsufficientBalance_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 1, price: 200m);
        _db.SeedAttendee(20, 10m);
        _db.CurrentUser.UserId = 20;
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 20,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateWaitlistService().ConvertAsync(_db.Waitlists.Items[0].Id));
    }

    [Test]
    public async Task GetMyWaitlist_FiltersByCurrentUserAndStatus()
    {
        _db.CurrentUser.UserId = 20;
        _db.Waitlists.Seed(
            new Waitlist { AttendeeId = 20, Status = WaitlistStatus.Waiting, RequestedAt = DateTime.UtcNow },
            new Waitlist { AttendeeId = 20, Status = WaitlistStatus.Converted, RequestedAt = DateTime.UtcNow },
            new Waitlist { AttendeeId = 21, Status = WaitlistStatus.Waiting, RequestedAt = DateTime.UtcNow });

        var page = await _db.CreateWaitlistService().GetMyWaitlistAsync(new WaitlistListQuery
        {
            Status = WaitlistStatus.Waiting
        });

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().AttendeeId, Is.EqualTo(20));
    }
}

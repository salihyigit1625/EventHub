using EventHub.Application.Services.Workers;
using EventHub.Application.Workers;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventHub.Tests.Services;

[TestFixture]
public class WorkerJobTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task EventCompletionJob_MarksPastPublishedEventsCompleted()
    {
        var past = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-10), status: EventStatus.Published);
        past.EndDate = DateTime.UtcNow.AddHours(-1);
        var future = _db.SeedEvent(start: DateTime.UtcNow.AddDays(1), status: EventStatus.Published);
        var cancelled = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-10), status: EventStatus.Cancelled);
        cancelled.EndDate = DateTime.UtcNow.AddHours(-1);

        var job = new EventCompletionJob(
            _db.Events,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 100 }),
            NullLogger<EventCompletionJob>.Instance);

        var count = await job.RunAsync();

        Assert.That(count, Is.EqualTo(1));
        Assert.That(past.Status, Is.EqualTo(EventStatus.Completed));
        Assert.That(future.Status, Is.EqualTo(EventStatus.Published));
        Assert.That(cancelled.Status, Is.EqualTo(EventStatus.Cancelled));
    }

    [Test]
    public async Task WaitlistHoldExpiryJob_ExpiresHoldsAndReleasesStock()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 10);
        _db.Waitlists.Seed(
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 20,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 21,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });

        var job = new WaitlistHoldExpiryJob(
            _db.Waitlists,
            _db.TicketTypes,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 100 }),
            NullLogger<WaitlistHoldExpiryJob>.Instance);

        var count = await job.RunAsync();

        Assert.That(count, Is.EqualTo(1));
        Assert.That(_db.Waitlists.Items[0].Status, Is.EqualTo(WaitlistStatus.Expired));
        Assert.That(_db.Waitlists.Items[1].Status, Is.EqualTo(WaitlistStatus.Notified));
        Assert.That(type.RemainingQuantity, Is.EqualTo(1));
    }

    [Test]
    public async Task WaitlistHoldExpiryJob_SecondRun_DoesNotDoubleRelease()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 10);
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = evt.Id,
            TicketTypeId = type.Id,
            AttendeeId = 20,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        });

        var job = new WaitlistHoldExpiryJob(
            _db.Waitlists,
            _db.TicketTypes,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 100 }),
            NullLogger<WaitlistHoldExpiryJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(1));
        Assert.That(await job.RunAsync(), Is.EqualTo(0));
        Assert.That(type.RemainingQuantity, Is.EqualTo(1));
    }

    [Test]
    public async Task EventCompletionJob_RespectsBatchSize()
    {
        for (var i = 0; i < 3; i++)
        {
            var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-10), status: EventStatus.Published);
            evt.EndDate = DateTime.UtcNow.AddHours(-1);
        }

        var job = new EventCompletionJob(
            _db.Events,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 2 }),
            NullLogger<EventCompletionJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(2));
        Assert.That(_db.Events.Items.Count(e => e.Status == EventStatus.Completed), Is.EqualTo(2));
        Assert.That(await job.RunAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task EventCompletionJob_NoDueEvents_ReturnsZero()
    {
        _db.SeedEvent(start: DateTime.UtcNow.AddDays(2), status: EventStatus.Published);

        var job = new EventCompletionJob(
            _db.Events,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 100 }),
            NullLogger<EventCompletionJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task WaitlistHoldExpiryJob_RespectsBatchSize()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0, total: 10);
        _db.Waitlists.Seed(
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 20,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 21,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 22,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });

        var job = new WaitlistHoldExpiryJob(
            _db.Waitlists,
            _db.TicketTypes,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 2 }),
            NullLogger<WaitlistHoldExpiryJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(2));
        Assert.That(type.RemainingQuantity, Is.EqualTo(2));
        Assert.That(await job.RunAsync(), Is.EqualTo(1));
        Assert.That(type.RemainingQuantity, Is.EqualTo(3));
    }

    [Test]
    public async Task WaitlistHoldExpiryJob_ZeroBatchSize_ClampsToOne()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 0);
        _db.Waitlists.Seed(
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 20,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Waitlist
            {
                EventId = evt.Id,
                TicketTypeId = type.Id,
                AttendeeId = 21,
                Status = WaitlistStatus.Notified,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });

        var job = new WaitlistHoldExpiryJob(
            _db.Waitlists,
            _db.TicketTypes,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 0 }),
            NullLogger<WaitlistHoldExpiryJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task WaitlistHoldExpiryJob_MissingTicketType_ExpiresEntryWithoutThrow()
    {
        _db.Waitlists.Seed(new Waitlist
        {
            EventId = 1,
            TicketTypeId = 999,
            AttendeeId = 20,
            Status = WaitlistStatus.Notified,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });

        var job = new WaitlistHoldExpiryJob(
            _db.Waitlists,
            _db.TicketTypes,
            _db.UnitOfWork,
            Options.Create(new WorkerOptions { BatchSize = 100 }),
            NullLogger<WaitlistHoldExpiryJob>.Instance);

        Assert.That(await job.RunAsync(), Is.EqualTo(1));
        Assert.That(_db.Waitlists.Items[0].Status, Is.EqualTo(WaitlistStatus.Expired));
    }
}

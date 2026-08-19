using EventHub.Application.DTOs.Events;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class EventServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Create_ApprovedOrganizer_SavesDraft()
    {
        _db.SeedApprovedOrganizer();
        _db.CurrentUser.UserId = 10;

        var created = await _db.CreateEventService().CreateAsync(new CreateEventDto
        {
            Title = "  Night Show  ",
            Venue = "  Dock  ",
            Description = "  Fun  ",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(11),
            CancellationDeadlineHours = 24
        });

        Assert.That(created.Status, Is.EqualTo(EventStatus.Draft));
        Assert.That(created.Title, Is.EqualTo("Night Show"));
        Assert.That(created.Venue, Is.EqualTo("Dock"));
        Assert.That(_db.Events.Items.Single().OrganizerId, Is.EqualTo(10));
    }

    [Test]
    public void Create_UnapprovedOrganizer_Throws()
    {
        _db.SeedPendingOrganizer(10);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().CreateAsync(ValidCreateDto()));
    }

    [Test]
    public void Create_MissingOrganizer_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateEventService().CreateAsync(ValidCreateDto()));
    }

    [Test]
    public void Create_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateEventService().CreateAsync(ValidCreateDto()));
    }

    [Test]
    public async Task Update_DoesNotCheckOrganizerOwnership()
    {
        _db.SeedApprovedOrganizer(10);
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 99;

        var updated = await _db.CreateEventService().UpdateAsync(evt.Id, new UpdateEventDto
        {
            Title = "Hijacked",
            Venue = "Other",
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CancellationDeadlineHours = 12
        });

        Assert.That(updated.Title, Is.EqualTo("Hijacked"));
    }

    [Test]
    public void Update_CancelledEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Cancelled);
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().UpdateAsync(evt.Id, new UpdateEventDto
            {
                Title = "X",
                Venue = "Y",
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                CancellationDeadlineHours = 1
            }));
    }

    [Test]
    public void Publish_WithoutTicketTypes_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().PublishAsync(evt.Id));
    }

    [Test]
    public void Publish_NonDraft_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Published);
        _db.SeedTicketType(evt.Id);
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().PublishAsync(evt.Id));
    }

    [Test]
    public async Task Publish_DraftWithTicketType_Succeeds()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        _db.SeedTicketType(evt.Id);

        var published = await _db.CreateEventService().PublishAsync(evt.Id);

        Assert.That(published.Status, Is.EqualTo(EventStatus.Published));
        Assert.That(published.TicketTypes, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Cancel_RefundsPaidTickets()
    {
        var evt = _db.SeedEvent(status: EventStatus.Published);
        var type = _db.SeedTicketType(evt.Id, remaining: 9, total: 10);
        var attendee = _db.SeedAttendee(20, 50m);
        var ticket = _db.SeedTicket(type.Id, attendee.UserId, price: 100m);
        _db.Payments.Seed(new Payment
        {
            TicketId = ticket.Id,
            AttendeeId = attendee.UserId,
            Amount = 100m,
            Status = PaymentStatus.Completed
        });

        await _db.CreateEventService().CancelAsync(evt.Id);

        Assert.That(evt.Status, Is.EqualTo(EventStatus.Cancelled));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Refunded));
        Assert.That(attendee.WalletBalance, Is.EqualTo(150m));
        Assert.That(_db.Payments.Items.Single().Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(_db.WalletTransactions.Items.Single().Type, Is.EqualTo(WalletTransactionType.Refund));
    }

    [Test]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Cancelled);
        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateEventService().CancelAsync(evt.Id));
    }

    [Test]
    public async Task GetById_ReturnsDraftWithoutPublishedFilter()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        var dto = await _db.CreateEventService().GetByIdAsync(evt.Id);
        Assert.That(dto.Status, Is.EqualTo(EventStatus.Draft));
    }

    [Test]
    public async Task GetPublished_FiltersSearchVenueAndDates()
    {
        var start = DateTime.UtcNow.AddDays(5);
        _db.Events.Seed(
            new Event
            {
                Title = "Jazz Night",
                Venue = "Istanbul Arena",
                StartDate = start,
                EndDate = start.AddHours(3),
                Status = EventStatus.Published,
                OrganizerId = 1
            },
            new Event
            {
                Title = "Jazz Night",
                Venue = "Ankara Hall",
                StartDate = start.AddDays(10),
                EndDate = start.AddDays(10).AddHours(3),
                Status = EventStatus.Published,
                OrganizerId = 1
            },
            new Event
            {
                Title = "Rock Fest",
                Venue = "Istanbul Arena",
                StartDate = start,
                EndDate = start.AddHours(3),
                Status = EventStatus.Published,
                OrganizerId = 1
            },
            new Event
            {
                Title = "Hidden Draft Jazz",
                Venue = "Istanbul Arena",
                StartDate = start,
                EndDate = start.AddHours(3),
                Status = EventStatus.Draft,
                OrganizerId = 1
            });

        var page = await _db.CreateEventService().GetPublishedAsync(new EventListQuery
        {
            Search = "jazz",
            Venue = "istanbul",
            From = start.AddDays(-1),
            To = start.AddDays(1),
            Page = 1,
            PageSize = 10
        });

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().Title, Is.EqualTo("Jazz Night"));
        Assert.That(page.Items.Single().Venue, Is.EqualTo("Istanbul Arena"));
    }

    [Test]
    public async Task GetPublished_ClampsPageSizeAndPages()
    {
        for (var i = 0; i < 12; i++)
        {
            _db.Events.Seed(new Event
            {
                Title = $"Event {i}",
                Venue = "Hall",
                StartDate = DateTime.UtcNow.AddDays(i + 1),
                EndDate = DateTime.UtcNow.AddDays(i + 1).AddHours(2),
                Status = EventStatus.Published,
                OrganizerId = 1
            });
        }

        var page = await _db.CreateEventService().GetPublishedAsync(new EventListQuery
        {
            Page = 2,
            PageSize = 99
        });

        Assert.That(page.Page, Is.EqualTo(2));
        Assert.That(page.PageSize, Is.EqualTo(10));
        Assert.That(page.Items, Has.Count.EqualTo(2));
        Assert.That(page.TotalCount, Is.EqualTo(12));
    }

    [Test]
    public async Task GetMyEvents_FiltersByCurrentOrganizerAndStatus()
    {
        _db.CurrentUser.UserId = 10;
        _db.Events.Seed(
            new Event
            {
                OrganizerId = 10,
                Title = "Mine Draft",
                Venue = "A",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = EventStatus.Draft
            },
            new Event
            {
                OrganizerId = 10,
                Title = "Mine Live",
                Venue = "A",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = EventStatus.Published
            },
            new Event
            {
                OrganizerId = 11,
                Title = "Other",
                Venue = "A",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = EventStatus.Published
            });

        var page = await _db.CreateEventService().GetMyEventsAsync(new EventListQuery
        {
            Status = EventStatus.Published
        });

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().Title, Is.EqualTo("Mine Live"));
    }

    [Test]
    public async Task UploadPoster_RejectsDisallowedExtension_AllowsDoubleExtensionPng()
    {
        var evt = _db.SeedEvent();
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
            {
                EventId = evt.Id,
                OriginalFileName = "payload.png.exe",
                ContentType = "image/png",
                Content = [1, 2, 3]
            }));

        var uploaded = await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "payload.exe.png",
            ContentType = "image/png",
            Content = [1, 2, 3]
        });

        Assert.That(uploaded.PosterDocumentId, Is.Not.Null);
        Assert.That(_db.Documents.Items.Single().StoredFileName, Is.EqualTo("payload.exe.png"));
    }

    [Test]
    public async Task GetPoster_ReadsStoredFile()
    {
        var evt = _db.SeedEvent();
        _db.CurrentUser.UserId = 10;
        await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "poster.jpg",
            ContentType = "image/jpeg",
            Content = [9, 8, 7]
        });

        var poster = await _db.CreateEventService().GetPosterAsync(evt.Id);

        Assert.That(poster.FileName, Is.EqualTo("poster.jpg"));
        Assert.That(poster.Content, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    [Test]
    public void GetPoster_WhenMissing_Throws()
    {
        var evt = _db.SeedEvent();
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().GetPosterAsync(evt.Id));
    }

    private static CreateEventDto ValidCreateDto() => new()
    {
        Title = "Show",
        Venue = "Hall",
        StartDate = DateTime.UtcNow.AddDays(2),
        EndDate = DateTime.UtcNow.AddDays(3)
    };
}

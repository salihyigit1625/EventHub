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

    private static readonly byte[] ValidPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
    ];

    private static readonly byte[] ValidJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

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
    public void Update_OtherOrganizer_Throws()
    {
        _db.SeedApprovedOrganizer(10);
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateEventService().UpdateAsync(evt.Id, new UpdateEventDto
            {
                Title = "Hijacked",
                Venue = "Other",
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                CancellationDeadlineHours = 12
            }));
    }

    [Test]
    public async Task Update_OwnerDraft_Succeeds()
    {
        _db.SeedApprovedOrganizer(10);
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;

        var updated = await _db.CreateEventService().UpdateAsync(evt.Id, new UpdateEventDto
        {
            Title = "Updated",
            Venue = "Hall",
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CancellationDeadlineHours = 12
        });

        Assert.That(updated.Title, Is.EqualTo("Updated"));
    }

    [Test]
    public void Update_PublishedEvent_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;

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
    public void Update_CancelledEvent_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Cancelled);
        _db.CurrentUser.UserId = 10;

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
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().PublishAsync(evt.Id));
    }

    [Test]
    public void Publish_NonDraft_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.SeedTicketType(evt.Id);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().PublishAsync(evt.Id));
    }

    [Test]
    public void Publish_OtherOrganizer_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.SeedTicketType(evt.Id);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateEventService().PublishAsync(evt.Id));
    }

    [Test]
    public async Task Publish_DraftWithTicketType_Succeeds()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.SeedTicketType(evt.Id);
        _db.CurrentUser.UserId = 10;

        var published = await _db.CreateEventService().PublishAsync(evt.Id);

        Assert.That(published.Status, Is.EqualTo(EventStatus.Published));
        Assert.That(published.TicketTypes, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Cancel_RefundsPaidTickets()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
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
        Assert.That(type.RemainingQuantity, Is.EqualTo(10));
        Assert.That(_db.Payments.Items.Single().Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(_db.WalletTransactions.Items.Single().Type, Is.EqualTo(WalletTransactionType.Refund));
    }

    [Test]
    public async Task Cancel_RefundsCheckedInTicketsAndRestoresStock()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 8, total: 10);
        var attendee = _db.SeedAttendee(20, 0m);
        var ticket = _db.SeedTicket(type.Id, attendee.UserId, TicketStatus.CheckedIn, price: 100m);
        _db.Payments.Seed(new Payment
        {
            TicketId = ticket.Id,
            AttendeeId = attendee.UserId,
            Amount = 100m,
            Status = PaymentStatus.Completed
        });

        await _db.CreateEventService().CancelAsync(evt.Id);

        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Refunded));
        Assert.That(attendee.WalletBalance, Is.EqualTo(100m));
        Assert.That(type.RemainingQuantity, Is.EqualTo(9));
    }

    [Test]
    public async Task Cancel_AfterTicketAlreadyCancelled_DoesNotDoubleRefund()
    {
        var evt = _db.SeedEvent(organizerId: 10, start: DateTime.UtcNow.AddDays(10), status: EventStatus.Published);
        _db.CurrentUser.UserId = 20;
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

        await _db.CreateTicketService().CancelAsync(ticket.Id);
        Assert.That(attendee.WalletBalance, Is.EqualTo(100m));

        _db.CurrentUser.UserId = 10;
        await _db.CreateEventService().CancelAsync(evt.Id);

        Assert.That(attendee.WalletBalance, Is.EqualTo(100m));
        Assert.That(_db.WalletTransactions.Items.Count(t => t.Type == WalletTransactionType.Refund), Is.EqualTo(1));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Cancelled));
    }

    [Test]
    public void Cancel_OtherOrganizer_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateEventService().CancelAsync(evt.Id));
    }

    [Test]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Cancelled);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() => _db.CreateEventService().CancelAsync(evt.Id));
    }

    [Test]
    public void GetById_Draft_Anonymous_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateEventService().GetByIdAsync(evt.Id));
    }

    [Test]
    public async Task GetById_Draft_Owner_Returns()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateEventService().GetByIdAsync(evt.Id);
        Assert.That(dto.Status, Is.EqualTo(EventStatus.Draft));
    }

    [Test]
    public async Task GetById_Published_Anonymous_Returns()
    {
        var evt = _db.SeedEvent(status: EventStatus.Published);
        var dto = await _db.CreateEventService().GetByIdAsync(evt.Id);
        Assert.That(dto.Status, Is.EqualTo(EventStatus.Published));
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
            Status = EventStatus.Draft,
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
    public void UploadPoster_RejectsDisallowedExtensionAndInvalidContent()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
            {
                EventId = evt.Id,
                OriginalFileName = "payload.png.exe",
                Content = ValidPng
            }));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
            {
                EventId = evt.Id,
                OriginalFileName = "payload.exe.png",
                Content = [0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
            }));
    }

    [Test]
    public async Task UploadPoster_StoresGuidNameAndRejectsTraversal()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        _db.CurrentUser.UserId = 10;

        var uploaded = await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "sub/../../poc.exe.png",
            Content = ValidPng
        });

        Assert.That(uploaded.PosterDocumentId, Is.Not.Null);
        var document = _db.Documents.Items.Single();
        Assert.That(document.StoredFileName, Does.Match("^[a-f0-9]{32}\\.png$"));
        Assert.That(document.OriginalFileName, Is.EqualTo("poc.exe.png"));
        Assert.That(_db.FileStorage.Files.ContainsKey(document.StoredFileName), Is.True);
    }

    [Test]
    public async Task UploadPoster_ReplacesPreviousPoster()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        _db.CurrentUser.UserId = 10;

        await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "first.png",
            Content = ValidPng
        });
        var firstStored = _db.Documents.Items.Single().StoredFileName;

        await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "second.jpg",
            Content = ValidJpeg
        });

        Assert.That(_db.Documents.Items, Has.Count.EqualTo(1));
        Assert.That(_db.FileStorage.Files.ContainsKey(firstStored), Is.False);
        Assert.That(_db.Documents.Items.Single().StoredFileName, Does.EndWith(".jpg"));
    }

    [Test]
    public void UploadPoster_OtherOrganizer_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
            {
                EventId = evt.Id,
                OriginalFileName = "poster.png",
                Content = ValidPng
            }));
    }

    [Test]
    public async Task GetPoster_Published_ReadsStoredFile()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
        await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "poster.jpg",
            Content = ValidJpeg
        });

        _db.CurrentUser.UserId = null;
        var poster = await _db.CreateEventService().GetPosterAsync(evt.Id);

        Assert.That(poster.ContentType, Is.EqualTo("image/jpeg"));
        Assert.That(poster.Content, Is.EqualTo(ValidJpeg));
    }

    [Test]
    public async Task GetPoster_Draft_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;
        await _db.CreateEventService().UploadPosterAsync(new UploadEventPosterDto
        {
            EventId = evt.Id,
            OriginalFileName = "poster.png",
            Content = ValidPng
        });

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateEventService().GetPosterAsync(evt.Id));
    }

    [Test]
    public void GetPoster_WhenMissing_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Published);
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

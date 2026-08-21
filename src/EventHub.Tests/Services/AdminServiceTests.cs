using EventHub.Application.Common;
using EventHub.Application.DTOs.Admin;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class AdminServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task ApproveOrganizer_SetsApprovalMetadata()
    {
        _db.SeedPendingOrganizer(11);
        _db.CurrentUser.UserId = 1;

        var dto = await _db.CreateAdminService().ApproveOrganizerAsync(11);

        Assert.That(dto.IsApproved, Is.True);
        Assert.That(_db.Organizers.Items.Single().ApprovedByAdminId, Is.EqualTo(1));
        Assert.That(_db.Organizers.Items.Single().ApprovedAt, Is.Not.Null);
    }

    [Test]
    public void ApproveOrganizer_AlreadyApproved_Throws()
    {
        _db.SeedApprovedOrganizer(11);
        _db.CurrentUser.UserId = 1;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().ApproveOrganizerAsync(11));
    }

    [Test]
    public async Task CreateGateStaff_WithAssignedPublishedEvent_CreatesUserAndProfile()
    {
        var evt = _db.SeedEvent();

        var dto = await _db.CreateAdminService().CreateGateStaffAsync(new CreateGateStaffDto
        {
            Email = "  Staff@EventHub.Local ",
            Password = "Staff123!",
            FullName = " Gate Person ",
            AssignedEventId = evt.Id
        });

        Assert.That(dto.Email, Is.EqualTo("staff@eventhub.local"));
        Assert.That(dto.FullName, Is.EqualTo("Gate Person"));
        Assert.That(dto.AssignedEventId, Is.EqualTo(evt.Id));
        Assert.That(dto.AssignedEventTitle, Is.EqualTo(evt.Title));
        Assert.That(_db.GateStaff.Items, Has.Count.EqualTo(1));
        Assert.That(_db.UserRoles.Items.Any(ur => ur.RoleId == 4), Is.True);
    }

    [Test]
    public void CreateGateStaff_DraftEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().CreateGateStaffAsync(new CreateGateStaffDto
            {
                Email = "staff@eventhub.local",
                Password = "Staff123!",
                FullName = "New",
                AssignedEventId = evt.Id
            }));
    }

    [Test]
    public void CreateGateStaff_DuplicateEmail_Throws()
    {
        _db.Users.Seed(new User { Email = "staff@eventhub.local", FullName = "Existing" });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().CreateGateStaffAsync(new CreateGateStaffDto
            {
                Email = "staff@eventhub.local",
                Password = "Staff123!",
                FullName = "New"
            }));
    }

    [Test]
    public void CreateGateStaff_UnknownEvent_Throws()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateAdminService().CreateGateStaffAsync(new CreateGateStaffDto
            {
                Email = "staff@eventhub.local",
                Password = "Staff123!",
                FullName = "New",
                AssignedEventId = 404
            }));
    }

    [Test]
    public async Task GetPendingApprovals_PagesUnapprovedOnly()
    {
        _db.SeedPendingOrganizer(11);
        _db.SeedPendingOrganizer(12);
        _db.SeedApprovedOrganizer(13);

        var page = await _db.CreateAdminService().GetPendingApprovalsAsync(new PagingQuery
        {
            Page = 1,
            PageSize = 1
        });

        Assert.That(page.TotalCount, Is.EqualTo(2));
        Assert.That(page.Items, Has.Count.EqualTo(1));
        Assert.That(page.Items[0].IsApproved, Is.False);
    }

    [Test]
    public async Task GetGlobalStats_AggregatesCounts()
    {
        _db.Users.Seed(new User { Email = "a@x.com" }, new User { Email = "b@x.com" });
        _db.Events.Seed(
            new Event { Title = "A", Venue = "V", Status = EventStatus.Published, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) },
            new Event { Title = "B", Venue = "V", Status = EventStatus.Draft, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) });
        _db.Tickets.Seed(
            new Ticket { UniqueCode = "1", Status = TicketStatus.Paid },
            new Ticket { UniqueCode = "2", Status = TicketStatus.CheckedIn },
            new Ticket { UniqueCode = "3", Status = TicketStatus.Cancelled });
        _db.Payments.Seed(
            new Payment { Amount = 40m, Status = PaymentStatus.Completed },
            new Payment { Amount = 10m, Status = PaymentStatus.Refunded });
        _db.Organizers.Seed(
            new OrganizerProfile { UserId = 50, CompanyName = "P", IsApproved = false },
            new OrganizerProfile { UserId = 51, CompanyName = "A", IsApproved = true });

        var stats = await _db.CreateAdminService().GetGlobalStatsAsync();

        Assert.That(stats.TotalUsers, Is.EqualTo(2));
        Assert.That(stats.TotalEvents, Is.EqualTo(2));
        Assert.That(stats.PublishedEvents, Is.EqualTo(1));
        Assert.That(stats.TicketsSold, Is.EqualTo(2));
        Assert.That(stats.TotalRevenue, Is.EqualTo(40m));
        Assert.That(stats.PendingOrganizers, Is.EqualTo(1));
    }

    [Test]
    public async Task AssignGateStaffToEvent_UpdatesAssignment()
    {
        var evt = _db.SeedEvent();
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30 });
        _db.UserRoles.Seed(new UserRole { UserId = 30, RoleId = 4 });

        var dto = await _db.CreateAdminService().AssignGateStaffToEventAsync(new AssignGateStaffDto
        {
            GateStaffUserId = 30,
            EventId = evt.Id
        });

        Assert.That(dto.AssignedEventId, Is.EqualTo(evt.Id));
        Assert.That(_db.GateStaff.Items.Single().AssignedEventId, Is.EqualTo(evt.Id));
    }

    [Test]
    public void AssignGateStaffToEvent_NonPublishedEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Cancelled);
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30 });
        _db.UserRoles.Seed(new UserRole { UserId = 30, RoleId = 4 });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().AssignGateStaffToEventAsync(new AssignGateStaffDto
            {
                GateStaffUserId = 30,
                EventId = evt.Id
            }));
    }

    [Test]
    public void AssignGateStaffToEvent_WithoutGateStaffRole_Throws()
    {
        var evt = _db.SeedEvent();
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30 });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().AssignGateStaffToEventAsync(new AssignGateStaffDto
            {
                GateStaffUserId = 30,
                EventId = evt.Id
            }));
    }

    [Test]
    public void AssignGateStaffToEvent_DraftEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Draft);
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30 });
        _db.UserRoles.Seed(new UserRole { UserId = 30, RoleId = 4 });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().AssignGateStaffToEventAsync(new AssignGateStaffDto
            {
                GateStaffUserId = 30,
                EventId = evt.Id
            }));
    }

    [Test]
    public void AssignGateStaffToEvent_CompletedEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Completed);
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30 });
        _db.UserRoles.Seed(new UserRole { UserId = 30, RoleId = 4 });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAdminService().AssignGateStaffToEventAsync(new AssignGateStaffDto
            {
                GateStaffUserId = 30,
                EventId = evt.Id
            }));
    }
}

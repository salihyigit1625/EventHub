using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class GateStaffServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task CheckIn_PaidTicketForAssignedEvent_Succeeds()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto
        {
            UniqueCode = "  SCANME ",
            DeviceLocation = "Gate A"
        });

        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.CheckedIn));
        Assert.That(ticket.CheckedInAt, Is.Not.Null);
        Assert.That(_db.CheckInLogs.Items.Single().IsSuccessful, Is.True);
        Assert.That(_db.CheckInLogs.Items.Single().DeviceLocation, Is.EqualTo("Gate A"));
    }

    [Test]
    public async Task CheckIn_WrongAssignedEvent_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = 999 });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto
        {
            UniqueCode = "SCANME"
        });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Ticket does not belong to the assigned event."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
        Assert.That(_db.CheckInLogs.Items.Single().IsSuccessful, Is.False);
    }

    [Test]
    public async Task CheckIn_NoAssignment_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = null });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto
        {
            UniqueCode = "SCANME"
        });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Gate staff is not assigned to an event."));
    }

    [Test]
    public async Task CheckIn_CancelledEvent_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1), status: EventStatus.Cancelled);
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto
        {
            UniqueCode = "SCANME"
        });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Event is not open for check-in."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
    }

    [Test]
    public async Task CheckIn_BeforeStart_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(2));
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "SCANME" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Check-in has not opened yet."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
    }

    [Test]
    public async Task CheckIn_AfterEnd_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-10));
        evt.EndDate = DateTime.UtcNow.AddHours(-1);
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "SCANME" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Check-in window has closed."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
    }

    [TestCase(TicketStatus.CheckedIn, "Ticket has already been checked in.")]
    [TestCase(TicketStatus.Cancelled, "Ticket has been cancelled.")]
    [TestCase(TicketStatus.Refunded, "Ticket has been refunded.")]
    [TestCase(TicketStatus.Reserved, "Ticket has not been paid.")]
    public async Task CheckIn_InvalidStatus_FailsAndLogs(TicketStatus status, string reason)
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, status, code: "CODE");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "CODE" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(reason));
        Assert.That(result.TicketId, Is.EqualTo(ticket.Id));
        Assert.That(ticket.Status, Is.EqualTo(status));
        Assert.That(_db.CheckInLogs.Items.Single().IsSuccessful, Is.False);
    }

    [Test]
    public async Task CheckIn_UnknownCode_FailsWithoutTicketId()
    {
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = 1 });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "NOPE" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Ticket was not found."));
        Assert.That(result.TicketId, Is.Null);
        Assert.That(_db.CheckInLogs.Items.Single().TicketId, Is.Null);
    }

    [Test]
    public void CheckIn_MissingProfile_Throws()
    {
        _db.CurrentUser.UserId = 30;
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "X" }));
    }

    [Test]
    public async Task GetAssignedEvent_ReturnsTitleWhenPresent()
    {
        var evt = _db.SeedEvent();
        _db.Users.Seed(new User { Id = 30, Email = "gate@eventhub.local", FullName = "Gate" });
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });
        _db.CurrentUser.UserId = 30;

        var dto = await _db.CreateGateStaffService().GetAssignedEventAsync();

        Assert.That(dto.AssignedEventId, Is.EqualTo(evt.Id));
        Assert.That(dto.AssignedEventTitle, Is.EqualTo(evt.Title));
    }

    [Test]
    public async Task CheckIn_CompletedEvent_Fails()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1), status: EventStatus.Completed);
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "SCANME" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Event is not open for check-in."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Paid));
    }

    [Test]
    public async Task CheckIn_ClaimFailsWhenAlreadyCheckedInMidFlow()
    {
        var evt = _db.SeedEvent(start: DateTime.UtcNow.AddHours(-1));
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, TicketStatus.Paid, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = evt.Id });

        var first = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "SCANME" });
        var second = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "SCANME" });

        Assert.That(first.IsSuccessful, Is.True);
        Assert.That(second.IsSuccessful, Is.False);
        Assert.That(second.FailureReason, Is.EqualTo("Ticket has already been checked in."));
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.CheckedIn));
    }
}

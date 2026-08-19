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
    public async Task CheckIn_PaidTicket_SucceedsRegardlessOfAssignedEvent()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id);
        var ticket = _db.SeedTicket(type.Id, 20, code: "SCANME");
        _db.CurrentUser.UserId = 30;
        _db.GateStaff.Seed(new GateStaffProfile { UserId = 30, AssignedEventId = 999 });

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

    [TestCase(TicketStatus.CheckedIn, "Ticket has already been checked in.")]
    [TestCase(TicketStatus.Cancelled, "Ticket has been cancelled.")]
    [TestCase(TicketStatus.Refunded, "Ticket has been refunded.")]
    [TestCase(TicketStatus.Reserved, "Ticket has not been paid.")]
    public async Task CheckIn_InvalidStatus_FailsAndLogs(TicketStatus status, string reason)
    {
        var ticket = _db.SeedTicket(1, 20, status, code: "CODE");
        _db.CurrentUser.UserId = 30;

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
        var result = await _db.CreateGateStaffService().CheckInAsync(new CheckInTicketDto { UniqueCode = "NOPE" });

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Ticket was not found."));
        Assert.That(result.TicketId, Is.Null);
        Assert.That(_db.CheckInLogs.Items.Single().TicketId, Is.Null);
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
}

using EventHub.Application.DTOs.Events;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class TicketTypeServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Create_SetsRemainingQuantityToTotal()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateTicketTypeService().CreateAsync(new CreateTicketTypeDto
        {
            EventId = evt.Id,
            Name = "  VIP  ",
            Price = 250m,
            TotalQuantity = 15,
            SaleStartDate = DateTime.UtcNow,
            SaleEndDate = DateTime.UtcNow.AddDays(5)
        });

        Assert.That(dto.Name, Is.EqualTo("VIP"));
        Assert.That(dto.RemainingQuantity, Is.EqualTo(15));
        Assert.That(dto.TotalQuantity, Is.EqualTo(15));
    }

    [Test]
    public void Create_OtherOrganizer_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateTicketTypeService().CreateAsync(new CreateTicketTypeDto
            {
                EventId = evt.Id,
                Name = "GA",
                Price = 10,
                TotalQuantity = 1,
                SaleStartDate = DateTime.UtcNow,
                SaleEndDate = DateTime.UtcNow.AddDays(1)
            }));
    }

    [Test]
    public void Create_CancelledEvent_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Cancelled);
        _db.CurrentUser.UserId = 10;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketTypeService().CreateAsync(new CreateTicketTypeDto
            {
                EventId = evt.Id,
                Name = "GA",
                Price = 10,
                TotalQuantity = 1,
                SaleStartDate = DateTime.UtcNow,
                SaleEndDate = DateTime.UtcNow.AddDays(1)
            }));
    }

    [Test]
    public void Create_UnknownEvent_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateTicketTypeService().CreateAsync(new CreateTicketTypeDto
            {
                EventId = 404,
                Name = "GA",
                Price = 10,
                TotalQuantity = 1,
                SaleStartDate = DateTime.UtcNow,
                SaleEndDate = DateTime.UtcNow.AddDays(1)
            }));
    }

    [Test]
    public async Task Update_RecalculatesRemainingFromSoldCount()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10);

        var updated = await _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
        {
            Name = "Updated",
            Price = 120m,
            TotalQuantity = 20,
            MaxTicketsPerUser = 5,
            SaleStartDate = type.SaleStartDate,
            SaleEndDate = type.SaleEndDate
        });

        Assert.That(updated.RemainingQuantity, Is.EqualTo(17));
        Assert.That(updated.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public void Update_Published_PriceChange_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10, price: 100m);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
            {
                Name = type.Name,
                Price = 150m,
                TotalQuantity = type.TotalQuantity,
                MaxTicketsPerUser = type.MaxTicketsPerUser,
                SaleStartDate = type.SaleStartDate,
                SaleEndDate = type.SaleEndDate
            }));
    }

    [Test]
    public void Update_Published_TotalQuantityChange_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
            {
                Name = type.Name,
                Price = type.Price,
                TotalQuantity = 20,
                MaxTicketsPerUser = type.MaxTicketsPerUser,
                SaleStartDate = type.SaleStartDate,
                SaleEndDate = type.SaleEndDate
            }));
    }

    [Test]
    public async Task Update_Published_NameAndSaleWindow_Succeeds()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Published);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10);
        var newEnd = type.SaleEndDate.AddDays(1);

        var updated = await _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
        {
            Name = "Renamed GA",
            Price = type.Price,
            TotalQuantity = type.TotalQuantity,
            MaxTicketsPerUser = 3,
            SaleStartDate = type.SaleStartDate,
            SaleEndDate = newEnd
        });

        Assert.That(updated.Name, Is.EqualTo("Renamed GA"));
        Assert.That(updated.MaxTicketsPerUser, Is.EqualTo(3));
        Assert.That(updated.SaleEndDate, Is.EqualTo(newEnd));
        Assert.That(updated.Price, Is.EqualTo(type.Price));
        Assert.That(updated.TotalQuantity, Is.EqualTo(10));
    }

    [Test]
    public void Update_OtherOrganizer_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10);
        _db.CurrentUser.UserId = 99;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
            {
                Name = "Updated",
                Price = 120m,
                TotalQuantity = 20,
                MaxTicketsPerUser = 5,
                SaleStartDate = type.SaleStartDate,
                SaleEndDate = type.SaleEndDate
            }));
    }

    [Test]
    public void Update_TotalBelowSold_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.CurrentUser.UserId = 10;
        var type = _db.SeedTicketType(evt.Id, remaining: 2, total: 10);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
            {
                Name = "GA",
                Price = 10,
                TotalQuantity = 7,
                MaxTicketsPerUser = 5,
                SaleStartDate = type.SaleStartDate,
                SaleEndDate = type.SaleEndDate
            }));
    }

    [Test]
    public async Task GetByEventId_Published_ReturnsTypes()
    {
        var first = _db.SeedEvent(status: EventStatus.Published);
        var second = _db.SeedEvent(status: EventStatus.Published);
        _db.SeedTicketType(first.Id);
        _db.SeedTicketType(first.Id);
        _db.SeedTicketType(second.Id);

        var list = await _db.CreateTicketTypeService().GetByEventIdAsync(first.Id);
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetByEventId_Draft_Anonymous_Throws()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.SeedTicketType(evt.Id);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateTicketTypeService().GetByEventIdAsync(evt.Id));
    }

    [Test]
    public async Task GetByEventId_Draft_Owner_Returns()
    {
        var evt = _db.SeedEvent(organizerId: 10, status: EventStatus.Draft);
        _db.SeedTicketType(evt.Id);
        _db.CurrentUser.UserId = 10;

        var list = await _db.CreateTicketTypeService().GetByEventIdAsync(evt.Id);
        Assert.That(list, Has.Count.EqualTo(1));
    }
}

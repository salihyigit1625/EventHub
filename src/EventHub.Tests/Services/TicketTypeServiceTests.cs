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
        var evt = _db.SeedEvent(status: EventStatus.Draft);

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
    public void Create_CancelledEvent_Throws()
    {
        var evt = _db.SeedEvent(status: EventStatus.Cancelled);
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
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 7, total: 10);

        var updated = await _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
        {
            Name = "Updated",
            Price = 120m,
            TotalQuantity = 20,
            SaleStartDate = type.SaleStartDate,
            SaleEndDate = type.SaleEndDate
        });

        Assert.That(updated.RemainingQuantity, Is.EqualTo(17));
        Assert.That(updated.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public void Update_TotalBelowSold_Throws()
    {
        var evt = _db.SeedEvent();
        var type = _db.SeedTicketType(evt.Id, remaining: 2, total: 10);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateTicketTypeService().UpdateAsync(type.Id, new UpdateTicketTypeDto
            {
                Name = "GA",
                Price = 10,
                TotalQuantity = 7,
                SaleStartDate = type.SaleStartDate,
                SaleEndDate = type.SaleEndDate
            }));
    }

    [Test]
    public async Task GetByEventId_ReturnsOnlyMatchingTypes()
    {
        var first = _db.SeedEvent();
        var second = _db.SeedEvent();
        _db.SeedTicketType(first.Id);
        _db.SeedTicketType(first.Id);
        _db.SeedTicketType(second.Id);

        var list = await _db.CreateTicketTypeService().GetByEventIdAsync(first.Id);
        Assert.That(list, Has.Count.EqualTo(2));
    }
}

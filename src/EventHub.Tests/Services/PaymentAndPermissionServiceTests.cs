using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class PaymentAndPermissionServiceTests
{
    [Test]
    public async Task PaymentService_GetByTicketId_DoesNotCheckCaller()
    {
        var db = new TestDb();
        db.Payments.Seed(new Payment
        {
            TicketId = 42,
            AttendeeId = 20,
            Amount = 75m,
            Status = PaymentStatus.Completed,
            TransactionCode = "tx"
        });

        var dto = await db.CreatePaymentService().GetByTicketIdAsync(42);

        Assert.That(dto.Amount, Is.EqualTo(75m));
        Assert.That(dto.TicketId, Is.EqualTo(42));
    }

    [Test]
    public void PaymentService_Missing_Throws()
    {
        var db = new TestDb();
        Assert.ThrowsAsync<KeyNotFoundException>(() => db.CreatePaymentService().GetByTicketIdAsync(1));
    }

    [Test]
    public async Task PermissionService_ReturnsCodesForUserRoles()
    {
        var db = new TestDb();
        db.UserRoles.Seed(new UserRole { UserId = 5, RoleId = 2 });
        db.Permissions.Seed(
            new Permission { Id = 1, Code = "events.create" },
            new Permission { Id = 2, Code = "tickets.purchase" });
        db.RolePermissions.Seed(new RolePermission { RoleId = 2, PermissionId = 1 });

        var service = db.CreatePermissionService();
        var codes = await service.GetPermissionsForUserAsync(5);

        Assert.That(codes, Is.EquivalentTo(new[] { "events.create" }));
        Assert.That(await service.UserHasPermissionAsync(5, "events.create"), Is.True);
        Assert.That(await service.UserHasPermissionAsync(5, "tickets.purchase"), Is.False);
    }

    [Test]
    public async Task PermissionService_UserWithoutRoles_ReturnsEmpty()
    {
        var db = new TestDb();
        db.Permissions.Seed(new Permission { Id = 1, Code = "events.create" });

        var codes = await db.CreatePermissionService().GetPermissionsForUserAsync(5);
        Assert.That(codes, Is.Empty);
    }
}

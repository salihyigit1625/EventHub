using EventHub.Application.Common;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class PaymentAndPermissionServiceTests
{
    [Test]
    public async Task PaymentService_GetMyPayments_ReturnsOnlyCurrentUser()
    {
        var db = new TestDb();
        db.CurrentUser.UserId = 20;
        db.Payments.Seed(
            new Payment
            {
                TicketId = 1,
                AttendeeId = 20,
                Amount = 75m,
                Status = PaymentStatus.Completed,
                TransactionCode = "mine",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Payment
            {
                TicketId = 2,
                AttendeeId = 21,
                Amount = 50m,
                Status = PaymentStatus.Completed,
                TransactionCode = "theirs",
                CreatedAt = DateTime.UtcNow
            });

        var page = await db.CreatePaymentService().GetMyPaymentsAsync(new PagingQuery { Page = 1, PageSize = 10 });

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().TransactionCode, Is.EqualTo("mine"));
        Assert.That(page.Items.Single().AttendeeId, Is.EqualTo(20));
    }

    [Test]
    public void PaymentService_GetMyPayments_Unauthenticated_Throws()
    {
        var db = new TestDb();
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            db.CreatePaymentService().GetMyPaymentsAsync(new PagingQuery()));
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

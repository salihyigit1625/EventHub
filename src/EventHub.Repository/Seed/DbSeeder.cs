using EventHub.Application.Common;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Repository.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Repository.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Roles.AnyAsync(cancellationToken))
        {
            await EnsureMissingPermissionsAsync(db, cancellationToken);
            return;
        }

        var adminRole = new Role { Name = AppRoles.Admin, Description = "System administrator" };
        var organizerRole = new Role { Name = AppRoles.Organizer, Description = "Event organizer" };
        var attendeeRole = new Role { Name = AppRoles.Attendee, Description = "Ticket buyer" };
        var gateStaffRole = new Role { Name = AppRoles.GateStaff, Description = "Gate check-in staff" };

        db.Roles.AddRange(adminRole, organizerRole, attendeeRole, gateStaffRole);
        await db.SaveChangesAsync(cancellationToken);

        var permissions = AppPermissions.All
            .Select(code => new Permission { Code = code, Description = code })
            .ToList();

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync(cancellationToken);

        var permissionIds = permissions.ToDictionary(p => p.Code, p => p.Id);

        Grant(adminRole, AppPermissions.All);
        Grant(organizerRole,
            AppPermissions.EventsCreate,
            AppPermissions.EventsUpdate,
            AppPermissions.EventsPublish,
            AppPermissions.EventsCancel,
            AppPermissions.EventsPosterUpload,
            AppPermissions.TicketTypesManage,
            AppPermissions.DocumentsUpload,
            AppPermissions.DocumentsDownload,
            AppPermissions.OrganizersView,
            AppPermissions.OrganizersUpdate,
            AppPermissions.WaitlistNotify);
        Grant(attendeeRole,
            AppPermissions.TicketsPurchase,
            AppPermissions.TicketsCancel,
            AppPermissions.TicketsView,
            AppPermissions.WaitlistJoin,
            AppPermissions.WaitlistConvert,
            AppPermissions.WalletView,
            AppPermissions.WalletDeposit,
            AppPermissions.PaymentsView,
            AppPermissions.DocumentsDownload,
            AppPermissions.OrganizersView);
        Grant(gateStaffRole,
            AppPermissions.TicketsCheckIn,
            AppPermissions.TicketsView);

        await db.SaveChangesAsync(cancellationToken);

        var hasher = new PasswordHasher<User>();

        var admin = CreateUser("admin@eventhub.local", "System Admin", hasher, "Admin123!");
        admin.UserRoles.Add(new UserRole { Role = adminRole });

        var organizer = CreateUser("organizer@eventhub.local", "Demo Organizer", hasher, "Organizer123!");
        organizer.UserRoles.Add(new UserRole { Role = organizerRole });
        organizer.OrganizerProfile = new OrganizerProfile
        {
            CompanyName = "Demo Events Co",
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow
        };

        var attendee = CreateUser("attendee@eventhub.local", "Demo Attendee", hasher, "Attendee123!");
        attendee.UserRoles.Add(new UserRole { Role = attendeeRole });
        attendee.AttendeeProfile = new AttendeeProfile { WalletBalance = 500m };

        var gateStaff = CreateUser("gatestaff@eventhub.local", "Demo Gate Staff", hasher, "GateStaff123!");
        gateStaff.UserRoles.Add(new UserRole { Role = gateStaffRole });
        gateStaff.GateStaffProfile = new GateStaffProfile();

        db.Users.AddRange(admin, organizer, attendee, gateStaff);
        await db.SaveChangesAsync(cancellationToken);

        organizer.OrganizerProfile!.ApprovedByAdminId = admin.Id;
        await db.SaveChangesAsync(cancellationToken);

        void Grant(Role role, params string[] codes)
        {
            foreach (var code in codes)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionIds[code]
                });
            }
        }
    }

    private static User CreateUser(string email, string fullName, PasswordHasher<User> hasher, string password)
    {
        var user = new User
        {
            Email = email,
            FullName = fullName,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }

    private static async Task EnsureMissingPermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(cancellationToken);
        var missingCodes = AppPermissions.All.Except(existingCodes).ToList();
        if (missingCodes.Count == 0)
            return;

        var permissions = missingCodes.Select(code => new Permission { Code = code, Description = code }).ToList();
        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync(cancellationToken);

        var organizerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Organizer, cancellationToken);
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin, cancellationToken);
        if (organizerRole is null || adminRole is null)
            return;

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permission.Id });
            if (permission.Code == AppPermissions.WaitlistNotify)
                db.RolePermissions.Add(new RolePermission { RoleId = organizerRole.Id, PermissionId = permission.Id });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

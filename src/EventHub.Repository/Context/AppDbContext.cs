using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Repository.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<OrganizerProfile> OrganizerProfiles => Set<OrganizerProfile>();
    public DbSet<AttendeeProfile> AttendeeProfiles => Set<AttendeeProfile>();
    public DbSet<GateStaffProfile> GateStaffProfiles => Set<GateStaffProfile>();

    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CheckInLog> CheckInLogs => Set<CheckInLog>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.Services.Admin;
using EventHub.Application.Services.Events;
using EventHub.Application.Services.Identity;
using EventHub.Application.Services.Profiles;
using EventHub.Application.Services.Ticketing;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Tests.Fakes;

public sealed class TestDb
{
    public CascadingUserRepository Users { get; }
    public InMemoryRepository<Role> Roles { get; } = new();
    public InMemoryRepository<UserRole> UserRoles { get; } = new();
    public InMemoryRepository<Permission> Permissions { get; } = new();
    public InMemoryRepository<RolePermission> RolePermissions { get; } = new();
    public InMemoryRepository<Event> Events { get; } = new();
    public InMemoryRepository<TicketType> TicketTypes { get; } = new();
    public InMemoryRepository<Ticket> Tickets { get; } = new();
    public InMemoryRepository<Waitlist> Waitlists { get; } = new();
    public InMemoryRepository<Payment> Payments { get; } = new();
    public InMemoryRepository<WalletTransaction> WalletTransactions { get; } = new();
    public InMemoryRepository<Document> Documents { get; } = new();
    public InMemoryRepository<CheckInLog> CheckInLogs { get; } = new();
    public InMemoryRepository<OrganizerProfile> Organizers { get; } = new();
    public InMemoryRepository<AttendeeProfile> Attendees { get; } = new();
    public InMemoryRepository<GateStaffProfile> GateStaff { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeCurrentUser CurrentUser { get; } = new();
    public FakeFileStorage FileStorage { get; } = new();
    public FakeTokenService TokenService { get; } = new();
    public FakePermissionService PermissionService { get; } = new();
    public IMapper Mapper { get; } = TestMapper.Create();

    public TestDb()
    {
        Users = new CascadingUserRepository(this);
        SeedDefaultRoles();
    }

    public AuthService CreateAuthService() => new(
        Users,
        UserRoles,
        Roles,
        TokenService,
        PermissionService,
        UnitOfWork,
        CurrentUser);

    public EventService CreateEventService() => new(
        Events,
        TicketTypes,
        Organizers,
        Tickets,
        Attendees,
        Payments,
        WalletTransactions,
        Documents,
        UnitOfWork,
        CurrentUser,
        FileStorage,
        Mapper);

    public TicketTypeService CreateTicketTypeService() => new(TicketTypes, Events, UnitOfWork, CurrentUser, Mapper);

    public TicketService CreateTicketService() => new(
        Tickets,
        TicketTypes,
        Events,
        Attendees,
        Payments,
        WalletTransactions,
        UnitOfWork,
        CurrentUser,
        Mapper);

    public WaitlistService CreateWaitlistService() => new(
        Waitlists,
        TicketTypes,
        Events,
        Attendees,
        Tickets,
        Payments,
        WalletTransactions,
        UnitOfWork,
        CurrentUser,
        Mapper);

    public WalletService CreateWalletService() => new(
        Attendees,
        WalletTransactions,
        UnitOfWork,
        CurrentUser,
        Mapper);

    public AdminService CreateAdminService() => new(
        Organizers,
        Users,
        Roles,
        UserRoles,
        Events,
        Tickets,
        Payments,
        GateStaff,
        UnitOfWork,
        CurrentUser);

    public DocumentService CreateDocumentService() => new(
        Documents,
        UnitOfWork,
        CurrentUser,
        FileStorage,
        Mapper);

    public OrganizerService CreateOrganizerService() => new(
        Organizers,
        Users,
        UnitOfWork,
        CurrentUser);

    public GateStaffService CreateGateStaffService() => new(
        Tickets,
        TicketTypes,
        CheckInLogs,
        GateStaff,
        Users,
        Events,
        UnitOfWork,
        CurrentUser);

    public PaymentService CreatePaymentService() => new(Payments, CurrentUser, Mapper);

    public PermissionService CreatePermissionService() => new(UserRoles, RolePermissions, Permissions);

    public OrganizerProfile SeedApprovedOrganizer(int userId = 10, string company = "Acme Events")
    {
        var profile = new OrganizerProfile
        {
            UserId = userId,
            CompanyName = company,
            IsApproved = true
        };
        Organizers.Seed(profile);
        Users.Seed(new User { Id = userId, Email = $"org{userId}@eventhub.local", FullName = "Organizer" });
        return profile;
    }

    public OrganizerProfile SeedPendingOrganizer(int userId = 11)
    {
        var profile = new OrganizerProfile
        {
            UserId = userId,
            CompanyName = "Pending Co",
            IsApproved = false
        };
        Organizers.Seed(profile);
        Users.Seed(new User { Id = userId, Email = $"pending{userId}@eventhub.local", FullName = "Pending Org" });
        return profile;
    }

    public AttendeeProfile SeedAttendee(int userId = 20, decimal balance = 500m)
    {
        var profile = new AttendeeProfile { UserId = userId, WalletBalance = balance };
        Attendees.Seed(profile);
        if (Users.Items.All(u => u.Id != userId))
            Users.Seed(new User { Id = userId, Email = $"attendee{userId}@eventhub.local", FullName = "Attendee" });
        return profile;
    }

    public Event SeedEvent(
        int organizerId = 10,
        EventStatus status = EventStatus.Published,
        DateTime? start = null,
        int cancellationDeadlineHours = 48,
        int? id = null)
    {
        var entity = new Event
        {
            OrganizerId = organizerId,
            Title = "Summer Fest",
            Venue = "Istanbul Arena",
            Description = "Outdoor concert",
            StartDate = start ?? DateTime.UtcNow.AddDays(30),
            EndDate = (start ?? DateTime.UtcNow.AddDays(30)).AddHours(6),
            Status = status,
            CancellationDeadlineHours = cancellationDeadlineHours
        };
        if (id is { } explicitId)
            entity.Id = explicitId;
        Events.Seed(entity);
        return entity;
    }

    public TicketType SeedTicketType(
        int eventId,
        int remaining = 10,
        int total = 10,
        decimal price = 100m,
        DateTime? saleStart = null,
        DateTime? saleEnd = null)
    {
        var type = new TicketType
        {
            EventId = eventId,
            Name = "General Admission",
            Price = price,
            TotalQuantity = total,
            RemainingQuantity = remaining,
            SaleStartDate = saleStart ?? DateTime.UtcNow.AddDays(-1),
            SaleEndDate = saleEnd ?? DateTime.UtcNow.AddDays(20)
        };
        TicketTypes.Seed(type);
        return type;
    }

    public Ticket SeedTicket(
        int ticketTypeId,
        int attendeeId,
        TicketStatus status = TicketStatus.Paid,
        decimal price = 100m,
        string? code = null)
    {
        var ticket = new Ticket
        {
            TicketTypeId = ticketTypeId,
            AttendeeId = attendeeId,
            UniqueCode = code ?? Guid.NewGuid().ToString("N"),
            UnitPrice = price,
            Status = status,
            PurchasedAt = DateTime.UtcNow
        };
        Tickets.Seed(ticket);
        return ticket;
    }

    private void SeedDefaultRoles()
    {
        Roles.Seed(
            new Role { Id = 1, Name = AppRoles.Admin },
            new Role { Id = 2, Name = AppRoles.Organizer },
            new Role { Id = 3, Name = AppRoles.Attendee },
            new Role { Id = 4, Name = AppRoles.GateStaff });
    }

    public sealed class CascadingUserRepository(TestDb db) : InMemoryRepository<User>
    {
        public override async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            await base.AddAsync(entity, cancellationToken);

            foreach (var userRole in entity.UserRoles)
            {
                userRole.UserId = entity.Id;
                await db.UserRoles.AddAsync(userRole, cancellationToken);
            }

            if (entity.AttendeeProfile is not null)
            {
                entity.AttendeeProfile.UserId = entity.Id;
                await db.Attendees.AddAsync(entity.AttendeeProfile, cancellationToken);
            }

            if (entity.OrganizerProfile is not null)
            {
                entity.OrganizerProfile.UserId = entity.Id;
                await db.Organizers.AddAsync(entity.OrganizerProfile, cancellationToken);
            }

            if (entity.GateStaffProfile is not null)
            {
                entity.GateStaffProfile.UserId = entity.Id;
                await db.GateStaff.AddAsync(entity.GateStaffProfile, cancellationToken);
            }
        }
    }
}

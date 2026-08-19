using EventHub.Application.Common;
using EventHub.Application.DTOs.Admin;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Application.Services.Admin;

public class AdminService(
    IGenericRepository<OrganizerProfile> organizerRepository,
    IGenericRepository<User> userRepository,
    IGenericRepository<Role> roleRepository,
    IGenericRepository<Event> eventRepository,
    IGenericRepository<Ticket> ticketRepository,
    IGenericRepository<Payment> paymentRepository,
    IGenericRepository<GateStaffProfile> gateStaffRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IAdminService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<OrganizerProfileDto> ApproveOrganizerAsync(
        int organizerUserId,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var profile = await organizerRepository.GetByIdAsync(organizerUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"OrganizerProfile ({organizerUserId}) was not found.");

        if (profile.IsApproved)
            throw new InvalidOperationException("This organizer is already approved.");

        var user = await userRepository.GetByIdAsync(organizerUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({organizerUserId}) was not found.");

        profile.IsApproved = true;
        profile.ApprovedByAdminId = adminId;
        profile.ApprovedAt = DateTime.UtcNow;

        organizerRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrganizerProfileDto
        {
            UserId = profile.UserId,
            Email = user.Email,
            FullName = user.FullName,
            CompanyName = profile.CompanyName,
            TaxNumber = profile.TaxNumber,
            IsApproved = profile.IsApproved,
            ApprovedAt = profile.ApprovedAt
        };
    }

    public async Task<GateStaffProfileDto> CreateGateStaffAsync(
        CreateGateStaffDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await userRepository.AnyAsync(u => u.Email == email, cancellationToken))
            throw new InvalidOperationException("A user with this email already exists.");

        var role = await roleRepository.FirstOrDefaultAsync(r => r.Name == AppRoles.GateStaff, cancellationToken)
            ?? throw new KeyNotFoundException($"Role '{AppRoles.GateStaff}' was not found.");

        string? assignedEventTitle = null;
        if (dto.AssignedEventId is { } eventId)
        {
            var assignedEvent = await eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");
            assignedEventTitle = assignedEvent.Title;
        }

        var user = new User
        {
            Email = email,
            FullName = dto.FullName.Trim(),
            IsActive = true,
            PasswordHash = _passwordHasher.HashPassword(null!, dto.Password),
            GateStaffProfile = new GateStaffProfile { AssignedEventId = dto.AssignedEventId }
        };

        user.UserRoles.Add(new UserRole { RoleId = role.Id });

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GateStaffProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AssignedEventId = dto.AssignedEventId,
            AssignedEventTitle = assignedEventTitle
        };
    }

    public async Task<IReadOnlyList<OrganizerProfileDto>> GetPendingApprovalsAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = await organizerRepository.FindAsync(p => !p.IsApproved, cancellationToken);
        var result = new List<OrganizerProfileDto>();

        foreach (var profile in profiles)
        {
            var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken);
            if (user is null)
                continue;

            result.Add(new OrganizerProfileDto
            {
                UserId = profile.UserId,
                Email = user.Email,
                FullName = user.FullName,
                CompanyName = profile.CompanyName,
                TaxNumber = profile.TaxNumber,
                IsApproved = profile.IsApproved,
                ApprovedAt = profile.ApprovedAt
            });
        }

        return result;
    }

    public async Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var events = await eventRepository.GetAllAsync(cancellationToken);
        var tickets = await ticketRepository.GetAllAsync(cancellationToken);
        var payments = await paymentRepository.GetAllAsync(cancellationToken);
        var organizers = await organizerRepository.GetAllAsync(cancellationToken);

        return new GlobalStatsDto
        {
            TotalUsers = users.Count,
            TotalEvents = events.Count,
            PublishedEvents = events.Count(e => e.Status == EventStatus.Published),
            TicketsSold = tickets.Count(t => t.Status is TicketStatus.Paid or TicketStatus.CheckedIn),
            TotalRevenue = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
            PendingOrganizers = organizers.Count(o => !o.IsApproved)
        };
    }

    public async Task<GateStaffProfileDto> AssignGateStaffToEventAsync(
        AssignGateStaffDto dto,
        CancellationToken cancellationToken = default)
    {
        var profile = await gateStaffRepository.GetByIdAsync(dto.GateStaffUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"GateStaffProfile ({dto.GateStaffUserId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(dto.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({dto.EventId}) was not found.");

        var user = await userRepository.GetByIdAsync(dto.GateStaffUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({dto.GateStaffUserId}) was not found.");

        profile.AssignedEventId = dto.EventId;
        gateStaffRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GateStaffProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AssignedEventId = eventEntity.Id,
            AssignedEventTitle = eventEntity.Title
        };
    }
}

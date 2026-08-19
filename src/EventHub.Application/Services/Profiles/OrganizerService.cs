using EventHub.Application.DTOs.Profiles;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;

namespace EventHub.Application.Services.Profiles;

public class OrganizerService(
    IGenericRepository<OrganizerProfile> organizerRepository,
    IGenericRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IOrganizerService
{
    public async Task<OrganizerProfileDto> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await organizerRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"OrganizerProfile ({userId}) was not found.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({userId}) was not found.");

        return MapToDto(profile, user);
    }

    public async Task<OrganizerProfileDto> UpdateProfileAsync(
        UpdateOrganizerProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var profile = await organizerRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"OrganizerProfile ({userId}) was not found.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({userId}) was not found.");

        profile.CompanyName = dto.CompanyName.Trim();
        profile.TaxNumber = string.IsNullOrWhiteSpace(dto.TaxNumber) ? null : dto.TaxNumber.Trim();

        organizerRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(profile, user);
    }

    private static OrganizerProfileDto MapToDto(OrganizerProfile profile, User user) => new()
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

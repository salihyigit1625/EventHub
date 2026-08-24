using EventHub.Application.DTOs.Profiles;

namespace EventHub.Application.Interfaces.Profiles;

public interface IOrganizerService
{
    Task<OrganizerProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<OrganizerProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<OrganizerProfileDto> UpdateProfileAsync(UpdateOrganizerProfileDto dto, CancellationToken cancellationToken = default);
}

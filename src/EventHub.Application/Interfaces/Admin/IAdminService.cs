using EventHub.Application.DTOs.Admin;
using EventHub.Application.DTOs.Profiles;

namespace EventHub.Application.Interfaces.Admin;

public interface IAdminService
{
    Task<OrganizerProfileDto> ApproveOrganizerAsync(int organizerUserId, CancellationToken cancellationToken = default);
    Task<GateStaffProfileDto> CreateGateStaffAsync(CreateGateStaffDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizerProfileDto>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken cancellationToken = default);
    Task<GateStaffProfileDto> AssignGateStaffToEventAsync(AssignGateStaffDto dto, CancellationToken cancellationToken = default);
}

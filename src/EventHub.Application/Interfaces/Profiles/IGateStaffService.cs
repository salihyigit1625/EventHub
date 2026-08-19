using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Profiles;

public interface IGateStaffService
{
    Task<CheckInResultDto> CheckInAsync(CheckInTicketDto dto, CancellationToken cancellationToken = default);
    Task<GateStaffProfileDto> GetAssignedEventAsync(CancellationToken cancellationToken = default);
}

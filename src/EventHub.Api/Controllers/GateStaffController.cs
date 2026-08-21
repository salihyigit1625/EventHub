using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/gate-staff")]
[Authorize(Roles = AppRoles.GateStaff)]
public class GateStaffController(IGateStaffService gateStaffService) : ControllerBase
{
    [HttpPost("check-in")]
    [HasPermission(AppPermissions.TicketsCheckIn)]
    [EnableRateLimiting("gate-staff-strict")]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(
        [FromBody] CheckInTicketDto dto,
        CancellationToken cancellationToken)
    {
        var result = await gateStaffService.CheckInAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("assigned-event")]
    [HasPermission(AppPermissions.TicketsCheckIn)]
    [EnableRateLimiting("gate-staff-relaxed")]
    public async Task<ActionResult<GateStaffProfileDto>> GetAssignedEvent(CancellationToken cancellationToken)
    {
        var result = await gateStaffService.GetAssignedEventAsync(cancellationToken);
        return Ok(result);
    }
}

using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Admin;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("organizers/pending")]
    [HasPermission(AppPermissions.AdminApprove)]
    [EnableRateLimiting("admin-relaxed")]
    public async Task<ActionResult<PagedResult<OrganizerProfileDto>>> GetPendingApprovals(
        [FromQuery] PagingQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetPendingApprovalsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("organizers/{userId:int}/approve")]
    [HasPermission(AppPermissions.AdminApprove)]
    [EnableRateLimiting("admin-strict")]
    public async Task<ActionResult<OrganizerProfileDto>> ApproveOrganizer(
        int userId,
        CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveOrganizerAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    [HasPermission(AppPermissions.AdminStats)]
    [EnableRateLimiting("admin-relaxed")]
    public async Task<ActionResult<GlobalStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        var result = await adminService.GetGlobalStatsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("gate-staff")]
    [HasPermission(AppPermissions.AdminGateStaff)]
    [EnableRateLimiting("admin-strict")]
    public async Task<ActionResult<GateStaffProfileDto>> CreateGateStaff(
        [FromBody] CreateGateStaffDto dto,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateGateStaffAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("gate-staff/assign")]
    [HasPermission(AppPermissions.AdminGateStaff)]
    [EnableRateLimiting("admin-strict")]
    public async Task<ActionResult<GateStaffProfileDto>> AssignGateStaff(
        [FromBody] AssignGateStaffDto dto,
        CancellationToken cancellationToken)
    {
        var result = await adminService.AssignGateStaffToEventAsync(dto, cancellationToken);
        return Ok(result);
    }
}

using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.Interfaces.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/organizers")]
public class OrganizersController(IOrganizerService organizerService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.OrganizersUpdate)]
    [EnableRateLimiting("organizers-relaxed")]
    public async Task<ActionResult<OrganizerProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await organizerService.GetMyProfileAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [HasPermission(AppPermissions.OrganizersView)]
    [EnableRateLimiting("organizers-relaxed")]
    public async Task<ActionResult<OrganizerProfileDto>> GetProfile(int userId, CancellationToken cancellationToken)
    {
        var result = await organizerService.GetProfileAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.OrganizersUpdate)]
    [EnableRateLimiting("organizers-strict")]
    public async Task<ActionResult<OrganizerProfileDto>> UpdateProfile(
        [FromBody] UpdateOrganizerProfileDto dto,
        CancellationToken cancellationToken)
    {
        var result = await organizerService.UpdateProfileAsync(dto, cancellationToken);
        return Ok(result);
    }
}

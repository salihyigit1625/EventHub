using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.Interfaces.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/organizers")]
public class OrganizersController(IOrganizerService organizerService) : ControllerBase
{
    [HttpGet("{userId:int}")]
    [Authorize]
    [HasPermission(AppPermissions.OrganizersView)]
    public async Task<ActionResult<OrganizerProfileDto>> GetProfile(int userId, CancellationToken cancellationToken)
    {
        var result = await organizerService.GetProfileAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.OrganizersUpdate)]
    public async Task<ActionResult<OrganizerProfileDto>> UpdateProfile(
        [FromBody] UpdateOrganizerProfileDto dto,
        CancellationToken cancellationToken)
    {
        var result = await organizerService.UpdateProfileAsync(dto, cancellationToken);
        return Ok(result);
    }
}

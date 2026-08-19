using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
[Authorize(Roles = AppRoles.Attendee)]
public class WaitlistController(IWaitlistService waitlistService) : ControllerBase
{
    [HttpPost("join/{ticketTypeId:int}")]
    [HasPermission(AppPermissions.WaitlistJoin)]
    public async Task<ActionResult<WaitlistDto>> Join(int ticketTypeId, CancellationToken cancellationToken)
    {
        var result = await waitlistService.JoinAsync(ticketTypeId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/convert")]
    [HasPermission(AppPermissions.WaitlistConvert)]
    public async Task<ActionResult<TicketDto>> Convert(int id, CancellationToken cancellationToken)
    {
        var result = await waitlistService.ConvertAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [HasPermission(AppPermissions.WaitlistJoin)]
    public async Task<ActionResult<IReadOnlyList<WaitlistDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await waitlistService.GetMyWaitlistAsync(cancellationToken);
        return Ok(result);
    }
}

using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController(IWaitlistService waitlistService) : ControllerBase
{
    [HttpPost("join/{ticketTypeId:int}")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.WaitlistJoin)]
    [EnableRateLimiting("waitlist-strict")]
    public async Task<ActionResult<WaitlistDto>> Join(int ticketTypeId, CancellationToken cancellationToken)
    {
        var result = await waitlistService.JoinAsync(ticketTypeId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ticket-types/{ticketTypeId:int}/notify-next")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.WaitlistNotify)]
    [EnableRateLimiting("waitlist-strict")]
    public async Task<ActionResult<WaitlistDto>> NotifyNext(int ticketTypeId, CancellationToken cancellationToken)
    {
        var result = await waitlistService.NotifyNextAsync(ticketTypeId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/convert")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.WaitlistConvert)]
    [EnableRateLimiting("waitlist-strict")]
    public async Task<ActionResult<TicketDto>> Convert(int id, CancellationToken cancellationToken)
    {
        var result = await waitlistService.ConvertAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.WaitlistJoin)]
    [EnableRateLimiting("waitlist-relaxed")]
    public async Task<ActionResult<PagedResult<WaitlistDto>>> GetMine(
        [FromQuery] WaitlistListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await waitlistService.GetMyWaitlistAsync(query, cancellationToken);
        return Ok(result);
    }
}

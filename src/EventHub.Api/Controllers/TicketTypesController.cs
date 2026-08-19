using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api")]
public class TicketTypesController(ITicketTypeService ticketTypeService) : ControllerBase
{
    [HttpGet("events/{eventId:int}/ticket-types")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TicketTypeDto>>> GetByEvent(
        int eventId,
        CancellationToken cancellationToken)
    {
        var result = await ticketTypeService.GetByEventIdAsync(eventId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ticket-types")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.TicketTypesManage)]
    public async Task<ActionResult<TicketTypeDto>> Create(
        [FromBody] CreateTicketTypeDto dto,
        CancellationToken cancellationToken)
    {
        var result = await ticketTypeService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("ticket-types/{id:int}")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.TicketTypesManage)]
    public async Task<ActionResult<TicketTypeDto>> Update(
        int id,
        [FromBody] UpdateTicketTypeDto dto,
        CancellationToken cancellationToken)
    {
        var result = await ticketTypeService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }
}

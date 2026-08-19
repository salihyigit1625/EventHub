using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    [HttpPost("purchase/{ticketTypeId:int}")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.TicketsPurchase)]
    public async Task<ActionResult<TicketDto>> Purchase(int ticketTypeId, CancellationToken cancellationToken)
    {
        var result = await ticketService.PurchaseAsync(ticketTypeId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.TicketsCancel)]
    public async Task<ActionResult<TicketDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await ticketService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.TicketsView)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await ticketService.GetMyTicketsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-code/{uniqueCode}")]
    [Authorize(Roles = $"{AppRoles.Attendee},{AppRoles.GateStaff}")]
    [HasPermission(AppPermissions.TicketsView)]
    public async Task<ActionResult<TicketDto>> GetByCode(string uniqueCode, CancellationToken cancellationToken)
    {
        var result = await ticketService.GetByCodeAsync(uniqueCode, cancellationToken);
        return Ok(result);
    }
}

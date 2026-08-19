using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("ticket/{ticketId:int}")]
    [Authorize(Roles = AppRoles.Attendee)]
    [HasPermission(AppPermissions.PaymentsView)]
    public async Task<ActionResult<PaymentDto>> GetByTicket(int ticketId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByTicketIdAsync(ticketId, cancellationToken);
        return Ok(result);
    }
}

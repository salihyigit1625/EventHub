using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = AppRoles.Attendee)]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("mine")]
    [HasPermission(AppPermissions.PaymentsView)]
    [EnableRateLimiting("wallet-relaxed")]
    public async Task<ActionResult<PagedResult<PaymentDto>>> GetMine(
        [FromQuery] PagingQuery query,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.GetMyPaymentsAsync(query, cancellationToken);
        return Ok(result);
    }
}

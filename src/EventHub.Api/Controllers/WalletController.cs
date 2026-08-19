using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize(Roles = AppRoles.Attendee)]
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpGet]
    [HasPermission(AppPermissions.WalletView)]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var result = await walletService.GetBalanceAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("deposit")]
    [HasPermission(AppPermissions.WalletDeposit)]
    public async Task<ActionResult<WalletBalanceDto>> Deposit(
        [FromBody] DepositDto dto,
        CancellationToken cancellationToken)
    {
        var result = await walletService.DepositAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("transactions")]
    [HasPermission(AppPermissions.WalletView)]
    public async Task<ActionResult<PagedResult<WalletTransactionDto>>> GetTransactions(
        [FromQuery] PagingQuery query,
        CancellationToken cancellationToken)
    {
        var result = await walletService.GetTransactionsAsync(query, cancellationToken);
        return Ok(result);
    }
}

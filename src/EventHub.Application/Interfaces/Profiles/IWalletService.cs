using EventHub.Application.Common;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Profiles;

public interface IWalletService
{
    Task<WalletBalanceDto> DepositAsync(DepositDto dto, CancellationToken cancellationToken = default);
    Task<WalletBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(PagingQuery query, CancellationToken cancellationToken = default);
}

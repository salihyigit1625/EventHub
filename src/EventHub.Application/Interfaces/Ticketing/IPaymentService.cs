using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetMyPaymentsAsync(PagingQuery query, CancellationToken cancellationToken = default);
}

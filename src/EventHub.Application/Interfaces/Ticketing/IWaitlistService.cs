using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface IWaitlistService
{
    Task<WaitlistDto> JoinAsync(int ticketTypeId, CancellationToken cancellationToken = default);
    Task<WaitlistDto> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default);
    Task<TicketDto> ConvertAsync(int waitlistId, CancellationToken cancellationToken = default);
    Task<PagedResult<WaitlistDto>> GetMyWaitlistAsync(WaitlistListQuery query, CancellationToken cancellationToken = default);
}

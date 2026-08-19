using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface ITicketService
{
    Task<TicketDto> PurchaseAsync(int ticketTypeId, CancellationToken cancellationToken = default);
    Task<TicketDto> CancelAsync(int ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(CancellationToken cancellationToken = default);
    Task<TicketDto> GetByCodeAsync(string uniqueCode, CancellationToken cancellationToken = default);
}

using EventHub.Application.DTOs.Events;

namespace EventHub.Application.Interfaces.Events;

public interface ITicketTypeService
{
    Task<TicketTypeDto> CreateAsync(CreateTicketTypeDto dto, CancellationToken cancellationToken = default);
    Task<TicketTypeDto> UpdateAsync(int ticketTypeId, UpdateTicketTypeDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketTypeDto>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}

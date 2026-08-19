using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface IPaymentService
{
    Task<PaymentDto> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);
}

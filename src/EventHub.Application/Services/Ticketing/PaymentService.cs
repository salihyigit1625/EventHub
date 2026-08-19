using AutoMapper;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Ticketing;

namespace EventHub.Application.Services.Ticketing;

public class PaymentService(
    IGenericRepository<Payment> paymentRepository,
    IMapper mapper) : IPaymentService
{
    public async Task<PaymentDto> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.FirstOrDefaultAsync(p => p.TicketId == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment for ticket ({ticketId}) was not found.");

        return mapper.Map<PaymentDto>(payment);
    }
}

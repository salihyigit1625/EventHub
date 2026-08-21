using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Domain.Entities.Ticketing;

namespace EventHub.Application.Services.Ticketing;

public class PaymentService(
    IGenericRepository<Payment> paymentRepository,
    ICurrentUserService currentUser,
    IMapper mapper) : IPaymentService
{
    public async Task<PagedResult<PaymentDto>> GetMyPaymentsAsync(
        PagingQuery query,
        CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var payments = await paymentRepository.FindAsync(p => p.AttendeeId == attendeeId, cancellationToken);
        var ordered = payments.OrderByDescending(p => p.CreatedAt).ToList();

        return new PagedResult<PaymentDto>
        {
            Items = ordered.Skip(query.Skip).Take(query.Take).Select(p => mapper.Map<PaymentDto>(p)).ToList(),
            Page = Math.Max(query.Page, 1),
            PageSize = query.Take,
            TotalCount = ordered.Count
        };
    }
}

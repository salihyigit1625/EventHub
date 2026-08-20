using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services.Events;

public class TicketTypeService(
    IGenericRepository<TicketType> ticketTypeRepository,
    IGenericRepository<Event> eventRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMapper mapper) : ITicketTypeService
{
    public async Task<TicketTypeDto> CreateAsync(
        CreateTicketTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await eventRepository.GetByIdAsync(dto.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({dto.EventId}) was not found.");

        EventOwnership.EnsureOwnedBy(eventEntity, currentUser.UserId);

        if (eventEntity.Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new InvalidOperationException("Ticket types cannot be added to cancelled or completed events.");

        var ticketType = mapper.Map<TicketType>(dto);
        await ticketTypeRepository.AddAsync(ticketType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TicketTypeDto>(ticketType);
    }

    public async Task<TicketTypeDto> UpdateAsync(
        int ticketTypeId,
        UpdateTicketTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"TicketType ({ticketTypeId}) was not found.");

        var eventEntity = await eventRepository.GetByIdAsync(ticketType.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({ticketType.EventId}) was not found.");

        EventOwnership.EnsureOwnedBy(eventEntity, currentUser.UserId);

        var soldQuantity = ticketType.TotalQuantity - ticketType.RemainingQuantity;
        if (dto.TotalQuantity < soldQuantity)
            throw new InvalidOperationException("Total quantity cannot be lower than the number of tickets already sold.");

        mapper.Map(dto, ticketType);
        ticketType.RemainingQuantity = dto.TotalQuantity - soldQuantity;

        ticketTypeRepository.Update(ticketType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TicketTypeDto>(ticketType);
    }

    public async Task<IReadOnlyList<TicketTypeDto>> GetByEventIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        var isOwner = currentUser.UserId is { } userId && eventEntity.OrganizerId == userId;
        if (eventEntity.Status != EventStatus.Published && !isOwner)
            throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == eventId, cancellationToken);
        return ticketTypes.Select(t => mapper.Map<TicketTypeDto>(t)).ToList();
    }
}

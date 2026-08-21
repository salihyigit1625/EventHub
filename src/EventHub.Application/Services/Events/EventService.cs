using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services.Events;

public class EventService(
    IGenericRepository<Event> eventRepository,
    IGenericRepository<TicketType> ticketTypeRepository,
    IGenericRepository<OrganizerProfile> organizerRepository,
    IGenericRepository<Ticket> ticketRepository,
    IGenericRepository<AttendeeProfile> attendeeRepository,
    IGenericRepository<Payment> paymentRepository,
    IGenericRepository<WalletTransaction> walletTransactionRepository,
    IGenericRepository<Document> documentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IFileStorageService fileStorage,
    IMapper mapper) : IEventService
{
    public async Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        var organizerId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var organizer = await organizerRepository.GetByIdAsync(organizerId, cancellationToken)
            ?? throw new KeyNotFoundException($"OrganizerProfile ({organizerId}) was not found.");

        if (!organizer.IsApproved)
            throw new InvalidOperationException("Organizer profile must be approved before creating events.");

        var entity = mapper.Map<Event>(dto);
        entity.OrganizerId = organizerId;
        entity.Status = EventStatus.Draft;

        await eventRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<EventDto>(entity);
    }

    public async Task<EventDto> UpdateAsync(
        int eventId,
        UpdateEventDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        EventOwnership.EnsureOwnedBy(entity, currentUser.UserId);

        if (entity.Status == EventStatus.Published)
            throw new InvalidOperationException("Published events cannot be updated.");

        if (entity.Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new InvalidOperationException("Cancelled or completed events cannot be updated.");

        mapper.Map(dto, entity);
        eventRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildEventDtoAsync(entity, cancellationToken);
    }

    public async Task<EventDto> PublishAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        EventOwnership.EnsureOwnedBy(entity, currentUser.UserId);

        if (entity.Status != EventStatus.Draft)
            throw new InvalidOperationException("Only draft events can be published.");

        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == eventId, cancellationToken);
        if (ticketTypes.Count == 0)
            throw new InvalidOperationException("An event must have at least one ticket type before publishing.");

        entity.Status = EventStatus.Published;
        eventRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildEventDtoAsync(entity, cancellationToken);
    }

    public async Task<EventDto> CancelAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        EventOwnership.EnsureOwnedBy(entity, currentUser.UserId);

        if (entity.Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new InvalidOperationException("This event cannot be cancelled.");

        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == eventId, cancellationToken);
        var ticketTypeIds = ticketTypes.Select(t => t.Id).ToList();
        var ticketTypesById = ticketTypes.ToDictionary(t => t.Id);

        var tickets = await ticketRepository.FindAsync(
            t => ticketTypeIds.Contains(t.TicketTypeId)
                 && (t.Status == TicketStatus.Paid || t.Status == TicketStatus.CheckedIn),
            cancellationToken);

        foreach (var ticket in tickets)
        {
            var claimed = TicketRefundGuard.TryClaimPaidTicket(ticket, TicketStatus.Refunded)
                          || TicketRefundGuard.TryClaimCheckedInTicket(ticket);
            if (!claimed)
                continue;

            ticketRepository.Update(ticket);

            if (ticketTypesById.TryGetValue(ticket.TicketTypeId, out var ticketType))
            {
                ticketType.RemainingQuantity++;
                ticketTypeRepository.Update(ticketType);
            }

            var payment = await paymentRepository.FirstOrDefaultAsync(p => p.TicketId == ticket.Id, cancellationToken);
            var shouldCreditWallet = TicketRefundGuard.TryClaimCompletedPayment(payment) || payment is null;
            if (!shouldCreditWallet)
            {
                if (payment is not null)
                    paymentRepository.Update(payment);
                continue;
            }

            var attendee = await attendeeRepository.GetByIdAsync(ticket.AttendeeId, cancellationToken);
            if (attendee is null)
                continue;

            WalletBalanceGuard.Credit(attendee, ticket.UnitPrice);
            attendeeRepository.Update(attendee);

            if (payment is not null)
                paymentRepository.Update(payment);

            await walletTransactionRepository.AddAsync(new WalletTransaction
            {
                AttendeeId = ticket.AttendeeId,
                Amount = ticket.UnitPrice,
                BalanceAfter = attendee.WalletBalance,
                Type = WalletTransactionType.Refund,
                PaymentId = payment?.Id,
                TicketId = ticket.Id
            }, cancellationToken);
        }

        entity.Status = EventStatus.Cancelled;
        eventRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildEventDtoAsync(entity, cancellationToken);
    }

    public async Task<EventDto> UploadPosterAsync(
        UploadEventPosterDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(dto.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({dto.EventId}) was not found.");

        EventOwnership.EnsureOwnedBy(entity, currentUser.UserId);

        var extension = PosterFileValidation.GetSafeExtension(dto.OriginalFileName);
        PosterFileValidation.EnsureMagicBytesMatch(dto.Content, extension);

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        if (entity.PosterDocumentId is { } existingPosterId)
        {
            var previous = await documentRepository.GetByIdAsync(existingPosterId, cancellationToken);
            if (previous is not null)
            {
                await fileStorage.DeleteAsync(previous.StoredFileName, cancellationToken);
                documentRepository.Remove(previous);
            }

            entity.PosterDocumentId = null;
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var contentType = PosterFileValidation.ContentTypeForExtension(extension);

        var stored = await fileStorage.SaveAsync(
            dto.Content,
            storedFileName,
            contentType,
            cancellationToken);

        var document = new Document
        {
            UploadedByUserId = userId,
            OriginalFileName = Path.GetFileName(dto.OriginalFileName),
            StoredFileName = stored.StoredFileName,
            FilePath = stored.FilePath,
            ContentType = stored.ContentType,
            FileSizeInBytes = stored.FileSizeInBytes
        };

        await documentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        entity.PosterDocumentId = document.Id;
        eventRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildEventDtoAsync(entity, cancellationToken);
    }

    public async Task<EventDto> GetByIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        if (entity.Status == EventStatus.Published)
            return await BuildEventDtoAsync(entity, cancellationToken);

        if (currentUser.UserId is { } userId && entity.OrganizerId == userId)
            return await BuildEventDtoAsync(entity, cancellationToken);

        throw new KeyNotFoundException($"Event ({eventId}) was not found.");
    }

    public async Task<PagedResult<EventListItemDto>> GetPublishedAsync(
        EventListQuery query,
        CancellationToken cancellationToken = default)
    {
        var publicQuery = new EventListQuery
        {
            Search = query.Search,
            Venue = query.Venue,
            From = query.From,
            To = query.To,
            Page = query.Page,
            PageSize = query.PageSize,
            Status = null
        };

        var predicate = BuildEventFilter(
            organizerId: null,
            requirePublished: true,
            publicQuery);

        var (items, totalCount) = await eventRepository.FindPagedAsync(
            predicate,
            e => e.StartDate,
            descending: false,
            query.Skip,
            query.Take,
            cancellationToken);

        return new PagedResult<EventListItemDto>
        {
            Items = items.Select(e => mapper.Map<EventListItemDto>(e)).ToList(),
            Page = Math.Max(query.Page, 1),
            PageSize = query.Take,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<EventListItemDto>> GetMyEventsAsync(
        EventListQuery query,
        CancellationToken cancellationToken = default)
    {
        var organizerId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var predicate = BuildEventFilter(
            organizerId,
            requirePublished: false,
            query);

        var (items, totalCount) = await eventRepository.FindPagedAsync(
            predicate,
            e => e.CreatedAt,
            descending: true,
            query.Skip,
            query.Take,
            cancellationToken);

        return new PagedResult<EventListItemDto>
        {
            Items = items.Select(e => mapper.Map<EventListItemDto>(e)).ToList(),
            Page = Math.Max(query.Page, 1),
            PageSize = query.Take,
            TotalCount = totalCount
        };
    }

    public async Task<(byte[] Content, string ContentType)> GetPosterAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        if (entity.Status != EventStatus.Published)
            throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        if (entity.PosterDocumentId is null)
            throw new InvalidOperationException("This event has no poster.");

        var document = await documentRepository.GetByIdAsync(entity.PosterDocumentId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Poster document was not found.");

        return await fileStorage.ReadAsync(document.StoredFileName, cancellationToken);
    }

    private async Task<EventDto> BuildEventDtoAsync(Event entity, CancellationToken cancellationToken)
    {
        var dto = mapper.Map<EventDto>(entity);
        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == entity.Id, cancellationToken);
        dto.TicketTypes = ticketTypes.Select(t => mapper.Map<TicketTypeDto>(t)).ToList();
        return dto;
    }

    private static System.Linq.Expressions.Expression<Func<Event, bool>> BuildEventFilter(
        int? organizerId,
        bool requirePublished,
        EventListQuery query)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim().ToLowerInvariant();
        var venue = string.IsNullOrWhiteSpace(query.Venue) ? null : query.Venue.Trim().ToLowerInvariant();
        var from = query.From;
        var to = query.To;
        var status = query.Status;

        return e =>
            (!requirePublished || e.Status == EventStatus.Published)
            && (organizerId == null || e.OrganizerId == organizerId.Value)
            && (status == null || e.Status == status.Value)
            && (search == null || e.Title.ToLowerInvariant().Contains(search))
            && (venue == null || e.Venue.ToLowerInvariant().Contains(venue))
            && (from == null || e.StartDate >= from.Value)
            && (to == null || e.StartDate <= to.Value);
    }
}

using AutoMapper;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
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

        if (entity.Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new InvalidOperationException("This event cannot be cancelled.");

        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == eventId, cancellationToken);
        var ticketTypeIds = ticketTypes.Select(t => t.Id).ToList();

        var tickets = await ticketRepository.FindAsync(
            t => ticketTypeIds.Contains(t.TicketTypeId) && t.Status == TicketStatus.Paid,
            cancellationToken);

        foreach (var ticket in tickets)
        {
            ticket.Status = TicketStatus.Refunded;
            ticketRepository.Update(ticket);

            var attendee = await attendeeRepository.GetByIdAsync(ticket.AttendeeId, cancellationToken);
            if (attendee is null)
                continue;

            attendee.WalletBalance += ticket.UnitPrice;
            attendee.UpdatedAt = DateTime.UtcNow;
            attendeeRepository.Update(attendee);

            var payment = await paymentRepository.FirstOrDefaultAsync(p => p.TicketId == ticket.Id, cancellationToken);
            if (payment is not null)
            {
                payment.Status = PaymentStatus.Refunded;
                paymentRepository.Update(payment);
            }

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

        ValidateFileExtension(dto.OriginalFileName);

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var stored = await fileStorage.SaveAsync(
            dto.Content,
            dto.OriginalFileName,
            dto.ContentType,
            cancellationToken);

        var document = new Document
        {
            UploadedByUserId = userId,
            OriginalFileName = dto.OriginalFileName,
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

        return await BuildEventDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<EventListItemDto>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.FindAsync(e => e.Status == EventStatus.Published, cancellationToken);
        return events.OrderBy(e => e.StartDate).Select(e => mapper.Map<EventListItemDto>(e)).ToList();
    }

    public async Task<IReadOnlyList<EventListItemDto>> GetMyEventsAsync(CancellationToken cancellationToken = default)
    {
        var organizerId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var events = await eventRepository.FindAsync(e => e.OrganizerId == organizerId, cancellationToken);
        return events.OrderByDescending(e => e.CreatedAt).Select(e => mapper.Map<EventListItemDto>(e)).ToList();
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> GetPosterAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event ({eventId}) was not found.");

        if (entity.PosterDocumentId is null)
            throw new InvalidOperationException("This event has no poster.");

        var document = await documentRepository.GetByIdAsync(entity.PosterDocumentId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Poster document was not found.");

        var (content, contentType) = await fileStorage.ReadAsync(document.StoredFileName, cancellationToken);
        return (content, contentType, document.OriginalFileName);
    }

    private async Task<EventDto> BuildEventDtoAsync(Event entity, CancellationToken cancellationToken)
    {
        var dto = mapper.Map<EventDto>(entity);
        var ticketTypes = await ticketTypeRepository.FindAsync(t => t.EventId == entity.Id, cancellationToken);
        dto.TicketTypes = ticketTypes.Select(t => mapper.Map<TicketTypeDto>(t)).ToList();
        return dto;
    }

    private static void ValidateFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!Common.FileUploadDefaults.AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only .png, .jpg and .pdf files are allowed.");
    }
}

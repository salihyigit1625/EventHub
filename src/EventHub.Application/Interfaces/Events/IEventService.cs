using EventHub.Application.DTOs.Events;

namespace EventHub.Application.Interfaces.Events;

public interface IEventService
{
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateAsync(int eventId, UpdateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> PublishAsync(int eventId, CancellationToken cancellationToken = default);
    Task<EventDto> CancelAsync(int eventId, CancellationToken cancellationToken = default);
    Task<EventDto> UploadPosterAsync(UploadEventPosterDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> GetByIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventListItemDto>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventListItemDto>> GetMyEventsAsync(CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> GetPosterAsync(int eventId, CancellationToken cancellationToken = default);
}

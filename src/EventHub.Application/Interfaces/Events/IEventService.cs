using EventHub.Application.DTOs.Events;
using EventHub.Application.Common;

namespace EventHub.Application.Interfaces.Events;

public interface IEventService
{
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateAsync(int eventId, UpdateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> PublishAsync(int eventId, CancellationToken cancellationToken = default);
    Task<EventDto> CancelAsync(int eventId, CancellationToken cancellationToken = default);
    Task<EventDto> UploadPosterAsync(UploadEventPosterDto dto, CancellationToken cancellationToken = default);
    Task<EventDto> GetByIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<PagedResult<EventListItemDto>> GetPublishedAsync(EventListQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<EventListItemDto>> GetMyEventsAsync(EventListQuery query, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> GetPosterAsync(int eventId, CancellationToken cancellationToken = default);
}

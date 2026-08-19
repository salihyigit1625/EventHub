using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events;

public class EventListItemDto
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EventStatus Status { get; set; }
    public int? PosterDocumentId { get; set; }
}

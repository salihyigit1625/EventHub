using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events;

public class EventDto
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Venue { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CancellationDeadlineHours { get; set; }
    public EventStatus Status { get; set; }
    public int? PosterDocumentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<TicketTypeDto> TicketTypes { get; set; } = [];
}

using EventHub.Domain.Common;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities.Events;

public class Event : BaseEntity
{
    public int OrganizerId { get; set; }
    public virtual User Organizer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Venue { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CancellationDeadlineHours { get; set; } = 48;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public string? PosterImageUrl { get; set; }

    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    public virtual ICollection<Waitlist> Waitlists { get; set; } = new List<Waitlist>();
}
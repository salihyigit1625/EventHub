using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities.Ticketing;

public class Waitlist
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public virtual Event Event { get; set; } = null!;

    public int TicketTypeId { get; set; }
    public virtual TicketType TicketType { get; set; } = null!;

    public int AttendeeId { get; set; }
    public virtual User Attendee { get; set; } = null!;

    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

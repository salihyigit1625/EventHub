using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class WaitlistDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int TicketTypeId { get; set; }
    public int AttendeeId { get; set; }
    public WaitlistStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

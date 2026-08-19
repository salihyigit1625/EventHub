using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class TicketDto
{
    public int Id { get; set; }
    public int TicketTypeId { get; set; }
    public int AttendeeId { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

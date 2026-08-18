using EventHub.Domain.Common;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities.Ticketing;

public class Ticket : BaseEntity
{
    public int TicketTypeId { get; set; }
    public virtual TicketType TicketType { get; set; } = null!;

    public int AttendeeId { get; set; }
    public virtual User Attendee { get; set; } = null!;

    public string UniqueCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Reserved;
    public DateTime? PurchasedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public virtual Payment? Payment { get; set; }
    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();
}
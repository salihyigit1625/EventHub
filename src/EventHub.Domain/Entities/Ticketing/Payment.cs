using EventHub.Domain.Common;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities.Ticketing;

public class Payment : BaseEntity
{
    public int TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public int AttendeeId { get; set; }
    public virtual User Attendee { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionCode { get; set; } = string.Empty;
}
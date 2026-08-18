using EventHub.Domain.Common;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities.Ticketing;

public class WalletTransaction : BaseEntity
{
    public int AttendeeId { get; set; }
    public virtual AttendeeProfile Attendee { get; set; } = null!;

    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public WalletTransactionType Type { get; set; }

    public int? PaymentId { get; set; }
    public virtual Payment? Payment { get; set; }

    public int? TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }
}

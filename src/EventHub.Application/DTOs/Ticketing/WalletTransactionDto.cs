using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class WalletTransactionDto
{
    public int Id { get; set; }
    public int AttendeeId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public WalletTransactionType Type { get; set; }
    public int? PaymentId { get; set; }
    public int? TicketId { get; set; }
    public DateTime CreatedAt { get; set; }
}

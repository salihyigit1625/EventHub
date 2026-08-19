using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class PaymentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AttendeeId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

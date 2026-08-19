namespace EventHub.Application.DTOs.Ticketing;

public class CheckInResultDto
{
    public bool IsSuccessful { get; set; }
    public string ScannedCode { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public int? TicketId { get; set; }
    public DateTime CheckedInAt { get; set; }
}

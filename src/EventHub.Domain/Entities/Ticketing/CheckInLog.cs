using EventHub.Domain.Entities.Identity;

namespace EventHub.Domain.Entities.Ticketing;

public class CheckInLog
{
    public int Id { get; set; }
    
    public int? TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }

    public int GateStaffId { get; set; }
    public virtual User GateStaff { get; set; } = null!;

    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string ScannedCode { get; set; } = string.Empty;
    public string? DeviceLocation { get; set; }
    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
}
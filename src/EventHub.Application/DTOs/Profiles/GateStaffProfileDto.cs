namespace EventHub.Application.DTOs.Profiles;

public class GateStaffProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? AssignedEventId { get; set; }
    public string? AssignedEventTitle { get; set; }
}

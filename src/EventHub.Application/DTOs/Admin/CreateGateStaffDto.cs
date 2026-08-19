namespace EventHub.Application.DTOs.Admin;

public class CreateGateStaffDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? AssignedEventId { get; set; }
}

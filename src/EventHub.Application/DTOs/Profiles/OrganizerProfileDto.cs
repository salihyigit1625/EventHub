namespace EventHub.Application.DTOs.Profiles;

public class OrganizerProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

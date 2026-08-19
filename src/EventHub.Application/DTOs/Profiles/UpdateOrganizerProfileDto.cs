namespace EventHub.Application.DTOs.Profiles;

public class UpdateOrganizerProfileDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
}

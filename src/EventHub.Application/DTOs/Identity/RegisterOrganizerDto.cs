namespace EventHub.Application.DTOs.Identity;

public class RegisterOrganizerDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
}

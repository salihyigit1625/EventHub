using EventHub.Domain.Entities.Identity;

namespace EventHub.Domain.Entities.Profiles;

public class OrganizerProfile
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsApproved { get; set; } = false;
    
    public int? ApprovedByAdminId { get; set; }
    public virtual User? ApprovedByAdmin { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
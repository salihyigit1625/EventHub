using EventHub.Domain.Common;
using EventHub.Domain.Entities.Profiles;

namespace EventHub.Domain.Entities.Identity;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual OrganizerProfile? OrganizerProfile { get; set; }
    public virtual AttendeeProfile? AttendeeProfile { get; set; }
    public virtual GateStaffProfile? GateStaffProfile { get; set; }
}
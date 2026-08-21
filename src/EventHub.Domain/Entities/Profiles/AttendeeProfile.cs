using EventHub.Domain.Entities.Identity;

namespace EventHub.Domain.Entities.Profiles;

public class AttendeeProfile
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public decimal WalletBalance { get; set; } = 0.00m;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

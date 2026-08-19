using EventHub.Domain.Entities.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Profiles;

public class AttendeeProfileConfiguration : IEntityTypeConfiguration<AttendeeProfile>
{
    public void Configure(EntityTypeBuilder<AttendeeProfile> builder)
    {
        builder.ToTable("AttendeeProfiles");

        builder.HasKey(a => a.UserId);

        builder.Property(a => a.WalletBalance)
            .HasPrecision(18, 2);

        builder.HasOne(a => a.User)
            .WithOne(u => u.AttendeeProfile)
            .HasForeignKey<AttendeeProfile>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

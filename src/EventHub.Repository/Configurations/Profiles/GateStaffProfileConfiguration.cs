using EventHub.Domain.Entities.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Profiles;

public class GateStaffProfileConfiguration : IEntityTypeConfiguration<GateStaffProfile>
{
    public void Configure(EntityTypeBuilder<GateStaffProfile> builder)
    {
        builder.ToTable("GateStaffProfiles");

        builder.HasKey(g => g.UserId);

        builder.HasOne(g => g.User)
            .WithOne(u => u.GateStaffProfile)
            .HasForeignKey<GateStaffProfile>(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.AssignedEvent)
            .WithMany()
            .HasForeignKey(g => g.AssignedEventId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

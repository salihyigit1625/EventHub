using EventHub.Domain.Entities.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Profiles;

public class OrganizerProfileConfiguration : IEntityTypeConfiguration<OrganizerProfile>
{
    public void Configure(EntityTypeBuilder<OrganizerProfile> builder)
    {
        builder.ToTable("OrganizerProfiles");

        builder.HasKey(o => o.UserId);

        builder.Property(o => o.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.TaxNumber)
            .HasMaxLength(50);

        builder.HasOne(o => o.User)
            .WithOne(u => u.OrganizerProfile)
            .HasForeignKey<OrganizerProfile>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(o => o.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

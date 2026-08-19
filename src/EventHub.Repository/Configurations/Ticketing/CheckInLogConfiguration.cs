using EventHub.Domain.Entities.Ticketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Ticketing;

public class CheckInLogConfiguration : IEntityTypeConfiguration<CheckInLog>
{
    public void Configure(EntityTypeBuilder<CheckInLog> builder)
    {
        builder.ToTable("CheckInLogs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FailureReason)
            .HasMaxLength(500);

        builder.Property(c => c.ScannedCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(c => c.DeviceLocation)
            .HasMaxLength(200);

        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.CheckInLogs)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.GateStaff)
            .WithMany()
            .HasForeignKey(c => c.GateStaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

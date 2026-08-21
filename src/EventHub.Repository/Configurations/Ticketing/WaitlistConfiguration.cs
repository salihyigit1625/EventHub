using EventHub.Domain.Entities.Ticketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Ticketing;

public class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.ToTable("Waitlists");

        builder.HasKey(w => w.Id);

        builder.HasOne(w => w.Event)
            .WithMany(e => e.Waitlists)
            .HasForeignKey(w => w.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.TicketType)
            .WithMany(tt => tt.Waitlists)
            .HasForeignKey(w => w.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Attendee)
            .WithMany()
            .HasForeignKey(w => w.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Waiting=1, Notified=2 — allow re-join after Expired/Converted.
        builder.HasIndex(w => new { w.EventId, w.TicketTypeId, w.AttendeeId })
            .IsUnique()
            .HasFilter("[Status] IN (1, 2)");
    }
}

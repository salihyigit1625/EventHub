using EventHub.Domain.Entities.Ticketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Ticketing;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.TransactionCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.TransactionCode)
            .IsUnique();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasOne(p => p.Ticket)
            .WithOne(t => t.Payment)
            .HasForeignKey<Payment>(p => p.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Attendee)
            .WithMany()
            .HasForeignKey(p => p.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

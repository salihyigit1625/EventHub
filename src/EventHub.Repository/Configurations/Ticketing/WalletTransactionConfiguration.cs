using EventHub.Domain.Entities.Ticketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Repository.Configurations.Ticketing;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Amount)
            .HasPrecision(18, 2);

        builder.Property(w => w.BalanceAfter)
            .HasPrecision(18, 2);

        builder.HasOne(w => w.Attendee)
            .WithMany()
            .HasForeignKey(w => w.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Payment)
            .WithMany()
            .HasForeignKey(w => w.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Ticket)
            .WithMany()
            .HasForeignKey(w => w.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

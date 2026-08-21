using EventHub.Domain.Entities.Profiles;

namespace EventHub.Application.Common;

public static class WalletBalanceGuard
{
    public static void Debit(AttendeeProfile attendee, decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Debit amount must be positive.");

        if (attendee.WalletBalance < amount)
            throw new InvalidOperationException("Insufficient wallet balance.");

        attendee.WalletBalance -= amount;
        attendee.UpdatedAt = DateTime.UtcNow;
    }

    public static void Credit(AttendeeProfile attendee, decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Credit amount must be positive.");

        attendee.WalletBalance += amount;
        attendee.UpdatedAt = DateTime.UtcNow;
    }
}

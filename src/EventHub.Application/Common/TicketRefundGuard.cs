using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common;

/// <summary>
/// Idempotent ticket/payment refund transitions to prevent double wallet credits.
/// </summary>
public static class TicketRefundGuard
{
    /// <summary>
    /// Marks a paid ticket with the terminal refund status. Returns false if already non-paid.
    /// </summary>
    public static bool TryClaimPaidTicket(Ticket ticket, TicketStatus terminalStatus)
    {
        if (ticket.Status != TicketStatus.Paid)
            return false;

        if (terminalStatus is not (TicketStatus.Cancelled or TicketStatus.Refunded))
            throw new ArgumentOutOfRangeException(nameof(terminalStatus));

        ticket.Status = terminalStatus;
        return true;
    }

    /// <summary>
    /// Marks a checked-in ticket as refunded (event-cancel path). Returns false if not checked-in.
    /// </summary>
    public static bool TryClaimCheckedInTicket(Ticket ticket)
    {
        if (ticket.Status != TicketStatus.CheckedIn)
            return false;

        ticket.Status = TicketStatus.Refunded;
        return true;
    }

    /// <summary>
    /// Transitions payment Completed → Refunded. Returns false if already refunded or not completed.
    /// </summary>
    public static bool TryClaimCompletedPayment(Payment? payment)
    {
        if (payment is null)
            return false;

        if (payment.Status != PaymentStatus.Completed)
            return false;

        payment.Status = PaymentStatus.Refunded;
        return true;
    }
}

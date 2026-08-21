using EventHub.Application.Common;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Tests.Common;

[TestFixture]
public class TicketRefundGuardTests
{
    [Test]
    public void TryClaimPaidTicket_OnlySucceedsOnce()
    {
        var ticket = new Ticket { Status = TicketStatus.Paid };

        Assert.That(TicketRefundGuard.TryClaimPaidTicket(ticket, TicketStatus.Cancelled), Is.True);
        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Cancelled));
        Assert.That(TicketRefundGuard.TryClaimPaidTicket(ticket, TicketStatus.Refunded), Is.False);
    }

    [Test]
    public void TryClaimCompletedPayment_OnlySucceedsOnce()
    {
        var payment = new Payment { Status = PaymentStatus.Completed };

        Assert.That(TicketRefundGuard.TryClaimCompletedPayment(payment), Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(TicketRefundGuard.TryClaimCompletedPayment(payment), Is.False);
    }

    [Test]
    public void TryClaimCompletedPayment_Null_ReturnsFalse()
    {
        Assert.That(TicketRefundGuard.TryClaimCompletedPayment(null), Is.False);
    }
}

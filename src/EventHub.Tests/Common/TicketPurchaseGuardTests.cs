using EventHub.Application.Common;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Common;

[TestFixture]
public class TicketPurchaseGuardTests
{
    [Test]
    public async Task EnsureUnderPerUserLimit_AtLimit_Throws()
    {
        var tickets = new InMemoryRepository<Ticket>();
        tickets.Seed(new Ticket
        {
            TicketTypeId = 1,
            AttendeeId = 20,
            Status = TicketStatus.Paid
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            TicketPurchaseGuard.EnsureUnderPerUserLimitAsync(tickets, 1, 20, maxTicketsPerUser: 1));
    }

    [Test]
    public async Task EnsureUnderPerUserLimit_CheckedInCountsTowardLimit()
    {
        var tickets = new InMemoryRepository<Ticket>();
        tickets.Seed(new Ticket
        {
            TicketTypeId = 1,
            AttendeeId = 20,
            Status = TicketStatus.CheckedIn
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            TicketPurchaseGuard.EnsureUnderPerUserLimitAsync(tickets, 1, 20, maxTicketsPerUser: 1));
    }

    [Test]
    public async Task EnsureUnderPerUserLimit_RefundedAndCancelledDoNotCount()
    {
        var tickets = new InMemoryRepository<Ticket>();
        tickets.Seed(
            new Ticket { TicketTypeId = 1, AttendeeId = 20, Status = TicketStatus.Cancelled },
            new Ticket { TicketTypeId = 1, AttendeeId = 20, Status = TicketStatus.Refunded });

        Assert.DoesNotThrowAsync(() =>
            TicketPurchaseGuard.EnsureUnderPerUserLimitAsync(tickets, 1, 20, maxTicketsPerUser: 1));
    }

    [Test]
    public async Task EnsureUnderPerUserLimit_UnderLimit_Succeeds()
    {
        var tickets = new InMemoryRepository<Ticket>();
        tickets.Seed(new Ticket
        {
            TicketTypeId = 1,
            AttendeeId = 20,
            Status = TicketStatus.Paid
        });

        Assert.DoesNotThrowAsync(() =>
            TicketPurchaseGuard.EnsureUnderPerUserLimitAsync(tickets, 1, 20, maxTicketsPerUser: 2));
    }
}

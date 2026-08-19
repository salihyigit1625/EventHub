using EventHub.Application.Common;
using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class TicketListQuery : PagingQuery
{
    public TicketStatus? Status { get; set; }
}

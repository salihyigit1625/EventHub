using EventHub.Application.Common;
using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Ticketing;

public class WaitlistListQuery : PagingQuery
{
    public WaitlistStatus? Status { get; set; }
}

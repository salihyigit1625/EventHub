using EventHub.Application.Common;
using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events;

public class EventListQuery : PagingQuery
{
    public string? Search { get; set; }
    public string? Venue { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public EventStatus? Status { get; set; }
}

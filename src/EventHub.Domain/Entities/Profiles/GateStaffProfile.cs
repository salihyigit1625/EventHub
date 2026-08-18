using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;

namespace EventHub.Domain.Entities.Profiles;

public class GateStaffProfile
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int? AssignedEventId { get; set; }
    public virtual Event? AssignedEvent { get; set; }
}
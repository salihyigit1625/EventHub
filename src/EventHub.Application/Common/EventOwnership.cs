using EventHub.Domain.Entities.Events;

namespace EventHub.Application.Common;

public static class EventOwnership
{
    public static void EnsureOwnedBy(Event entity, int? userId)
    {
        if (userId is null)
            throw new UnauthorizedAccessException("Authentication is required.");

        if (entity.OrganizerId != userId.Value)
            throw new UnauthorizedAccessException("You do not own this event.");
    }
}

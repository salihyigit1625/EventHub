using EventHub.Application.Common;
using EventHub.Domain.Entities.Events;

namespace EventHub.Tests.Common;

[TestFixture]
public class EventOwnershipTests
{
    [Test]
    public void EnsureOwnedBy_Owner_Succeeds()
    {
        var entity = new Event { OrganizerId = 10 };
        Assert.DoesNotThrow(() => EventOwnership.EnsureOwnedBy(entity, 10));
    }

    [Test]
    public void EnsureOwnedBy_Unauthenticated_Throws()
    {
        var entity = new Event { OrganizerId = 10 };
        Assert.Throws<UnauthorizedAccessException>(() => EventOwnership.EnsureOwnedBy(entity, null));
    }

    [Test]
    public void EnsureOwnedBy_OtherUser_Throws()
    {
        var entity = new Event { OrganizerId = 10 };
        Assert.Throws<UnauthorizedAccessException>(() => EventOwnership.EnsureOwnedBy(entity, 99));
    }
}

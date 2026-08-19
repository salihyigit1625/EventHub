using System.Security.Claims;
using EventHub.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EventHub.Tests.Api;

[TestFixture]
public class PermissionAuthorizationHandlerTests
{
    [Test]
    public async Task Succeeds_WhenPermissionClaimMatches()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "events.create")],
            authenticationType: "test"));
        var requirement = new PermissionRequirement("events.create");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task DoesNotSucceed_WhenPermissionIsMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "tickets.view")],
            authenticationType: "test"));
        var requirement = new PermissionRequirement("events.create");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task DoesNotSucceed_ForAnonymousUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var requirement = new PermissionRequirement("events.create");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }
}

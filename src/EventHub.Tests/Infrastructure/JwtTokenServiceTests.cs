using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventHub.Domain.Entities.Identity;
using EventHub.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace EventHub.Tests.Infrastructure;

[TestFixture]
public class JwtTokenServiceTests
{
    [Test]
    public void GenerateAccessToken_IncludesRolesAndPermissions()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            SecretKey = "EventHubDevSecretKey_ChangeMe_32chars!",
            Issuer = "EventHub",
            Audience = "EventHubClients",
            ExpiresInMinutes = 15
        }));

        var token = service.GenerateAccessToken(
            new User { Id = 7, Email = "ada@eventhub.local", FullName = "Ada" },
            ["Attendee"],
            ["tickets.purchase", "wallet.view"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Issuer, Is.EqualTo("EventHub"));
        Assert.That(jwt.Audiences, Does.Contain("EventHubClients"));
        Assert.That(jwt.Subject, Is.EqualTo("7"));
        Assert.That(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "ada@eventhub.local"), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type == "fullName" && c.Value == "Ada"), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type is ClaimTypes.Role or "role" && c.Value == "Attendee"), Is.True);
        Assert.That(jwt.Claims.Count(c => c.Type == "permission"), Is.EqualTo(2));
        Assert.That(jwt.ValidTo, Is.GreaterThan(DateTime.UtcNow.AddMinutes(10)));
        Assert.That(jwt.ValidTo, Is.LessThan(DateTime.UtcNow.AddMinutes(20)));
    }

    [Test]
    public void GenerateRefreshToken_ReturnsUniqueValues()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            SecretKey = "EventHubDevSecretKey_ChangeMe_32chars!",
            Issuer = "EventHub",
            Audience = "EventHubClients"
        }));

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(Convert.FromBase64String(first), Has.Length.EqualTo(64));
    }
}

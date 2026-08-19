using EventHub.Domain.Entities.Identity;

namespace EventHub.Application.Interfaces.Identity;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles, IList<string> permissions);
    string GenerateRefreshToken();
}

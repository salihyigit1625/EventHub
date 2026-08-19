using System.Security.Claims;
using EventHub.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Http;

namespace EventHub.Infrastructure.Identity;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return int.TryParse(value, out var id) ? id : null;
        }
    }
}

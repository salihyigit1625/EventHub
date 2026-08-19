using Microsoft.AspNetCore.Authorization;

namespace EventHub.Api.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = permission;
    }
}

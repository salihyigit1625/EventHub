using Microsoft.AspNetCore.Authorization;

namespace EventHub.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

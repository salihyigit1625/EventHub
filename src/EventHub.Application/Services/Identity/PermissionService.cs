using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Domain.Entities.Identity;

namespace EventHub.Application.Services.Identity;

public class PermissionService(
    IGenericRepository<UserRole> userRoleRepository,
    IGenericRepository<RolePermission> rolePermissionRepository,
    IGenericRepository<Permission> permissionRepository) : IPermissionService
{
    public async Task<IList<string>> GetPermissionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var userRoles = await userRoleRepository.FindAsync(ur => ur.UserId == userId, cancellationToken);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

        var rolePermissions = await rolePermissionRepository.FindAsync(
            rp => roleIds.Contains(rp.RoleId), cancellationToken);
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        var permissions = await permissionRepository.GetAllAsync(cancellationToken);
        return permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Code).ToList();
    }

    public async Task<bool> UserHasPermissionAsync(
        int userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsForUserAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }
}

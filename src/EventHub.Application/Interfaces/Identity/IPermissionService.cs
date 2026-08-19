namespace EventHub.Application.Interfaces.Identity;

public interface IPermissionService
{
    Task<IList<string>> GetPermissionsForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UserHasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken = default);
}

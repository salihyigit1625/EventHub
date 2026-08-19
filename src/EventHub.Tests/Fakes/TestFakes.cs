using AutoMapper;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Storage;
using EventHub.Application.Mappings;
using EventHub.Domain.Entities.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventHub.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }

    public void Dispose()
    {
    }
}

public sealed class FakeCurrentUser : ICurrentUserService
{
    public int? UserId { get; set; }
}

public sealed class FakeFileStorage : IFileStorageService
{
    public Dictionary<string, (byte[] Content, string ContentType, string FilePath)> Files { get; } = new();

    public Task<(string StoredFileName, string FilePath, string ContentType, long FileSizeInBytes)> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine("uploads", originalFileName);
        Files[originalFileName] = (content, contentType, path);
        return Task.FromResult((originalFileName, path, contentType, (long)content.Length));
    }

    public Task<(byte[] Content, string ContentType)> ReadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!Files.TryGetValue(fileName, out var stored))
            throw new FileNotFoundException(fileName);

        return Task.FromResult((stored.Content, stored.ContentType));
    }
}

public sealed class FakeTokenService : ITokenService
{
    public int AccessTokenCalls { get; private set; }
    public int RefreshTokenCalls { get; private set; }
    public IList<string>? LastRoles { get; private set; }
    public IList<string>? LastPermissions { get; private set; }

    public string GenerateAccessToken(User user, IList<string> roles, IList<string> permissions)
    {
        AccessTokenCalls++;
        LastRoles = roles.ToList();
        LastPermissions = permissions.ToList();
        return $"access-{user.Id}";
    }

    public string GenerateRefreshToken()
    {
        RefreshTokenCalls++;
        return $"refresh-{RefreshTokenCalls}";
    }
}

public sealed class FakePermissionService : IPermissionService
{
    public IList<string> Permissions { get; set; } = ["tickets.purchase"];

    public Task<IList<string>> GetPermissionsForUserAsync(int userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Permissions);

    public Task<bool> UserHasPermissionAsync(
        int userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Permissions.Contains(permissionCode));
}

public static class TestMapper
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }
}

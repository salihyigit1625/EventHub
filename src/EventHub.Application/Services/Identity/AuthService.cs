using EventHub.Application.Common;
using EventHub.Application.DTOs.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Identity;
using EventHub.Domain.Entities.Profiles;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Application.Services.Identity;

public class AuthService(
    IGenericRepository<User> userRepository,
    IGenericRepository<UserRole> userRoleRepository,
    IGenericRepository<Role> roleRepository,
    ITokenService tokenService,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResponseDto> RegisterAttendeeAsync(
        RegisterAttendeeDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await CreateUserAsync(dto.Email, dto.FullName, dto.Password, AppRoles.Attendee, cancellationToken);
        user.AttendeeProfile = new AttendeeProfile();

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GenerateTokensAsync(user, [AppRoles.Attendee], cancellationToken);
    }

    public async Task<AuthResponseDto> RegisterOrganizerAsync(
        RegisterOrganizerDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await CreateUserAsync(dto.Email, dto.FullName, dto.Password, AppRoles.Organizer, cancellationToken);
        user.OrganizerProfile = new OrganizerProfile
        {
            CompanyName = dto.CompanyName.Trim(),
            TaxNumber = string.IsNullOrWhiteSpace(dto.TaxNumber) ? null : dto.TaxNumber.Trim(),
            IsApproved = false
        };

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GenerateTokensAsync(user, [AppRoles.Organizer], cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = await GetRoleNamesAsync(user.Id, cancellationToken);
        return await GenerateTokensAsync(user, roles, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            u => u.RefreshToken == dto.RefreshToken,
            cancellationToken);

        if (user is null || user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account is inactive.");

        var roles = await GetRoleNamesAsync(user.Id, cancellationToken);
        return await GenerateTokensAsync(user, roles, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({userId}) was not found.");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User ({userId}) was not found.");

        var roles = await GetRoleNamesAsync(user.Id, cancellationToken);

        return new CurrentUserDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            Roles = roles
        };
    }

    private async Task<AuthResponseDto> GenerateTokensAsync(
        User user,
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetPermissionsForUserAsync(user.Id, cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiresAt;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        };
    }

    private async Task<IList<string>> GetRoleNamesAsync(int userId, CancellationToken cancellationToken)
    {
        var userRoles = await userRoleRepository.FindAsync(ur => ur.UserId == userId, cancellationToken);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToHashSet();
        var roles = await roleRepository.GetAllAsync(cancellationToken);
        return roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();
    }

    private async Task<User> CreateUserAsync(
        string email,
        string fullName,
        string password,
        string roleName,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await userRepository.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            throw new InvalidOperationException("A user with this email already exists.");

        var role = await roleRepository.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken)
            ?? throw new KeyNotFoundException($"Role '{roleName}' was not found.");

        var user = new User
        {
            Email = normalizedEmail,
            FullName = fullName.Trim(),
            IsActive = true,
            PasswordHash = _passwordHasher.HashPassword(null!, password)
        };

        user.UserRoles.Add(new UserRole { RoleId = role.Id });
        return user;
    }
}

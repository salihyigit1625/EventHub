using EventHub.Application.DTOs.Identity;

namespace EventHub.Application.Interfaces.Identity;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAttendeeAsync(RegisterAttendeeDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RegisterOrganizerAsync(RegisterOrganizerDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

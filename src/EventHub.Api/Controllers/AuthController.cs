using EventHub.Application.DTOs.Identity;
using EventHub.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register/attendee")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAttendee(
        [FromBody] RegisterAttendeeDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAttendeeAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register/organizer")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrganizer(
        [FromBody] RegisterOrganizerDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterOrganizerAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody] RefreshTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("auth-relaxed")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("auth-relaxed")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(cancellationToken);
        return Ok(result);
    }
}

using EventHub.Application.DTOs.Identity;
using EventHub.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register/attendee")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RegisterAttendee(
        [FromBody] RegisterAttendeeDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAttendeeAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register/organizer")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrganizer(
        [FromBody] RegisterOrganizerDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterOrganizerAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody] RefreshTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(cancellationToken);
        return Ok(result);
    }
}

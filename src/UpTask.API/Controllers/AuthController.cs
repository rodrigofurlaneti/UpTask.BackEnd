using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Features.Auth.Commands;
using UpTask.Application.Features.Auth.DTOs;
namespace UpTask.API.Controllers;

/// <summary>Handles user registration, login, and password management.</summary>
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ApiController(sender)
{
    /// <summary>Creates a new user account and returns a JWT token.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthTokenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess) return Ok(result); // reuses Problem translation
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Authenticates a user and returns a JWT token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Changes the current user's password.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return NoContent(result);
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Auth.Commands;

namespace UpTask.API.Controllers
{
    // ── AUTH ──────────────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [Route("api/v1/auth")]
    public sealed class AuthController(ISender mediator, ICurrentUserService currentUser)
        : ApiController(mediator, currentUser)
    {
        /// <summary>Register a new account</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 422)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new RegisterCommand(dto.Name, dto.Email, dto.Password, dto.ConfirmPassword), ct);
            return Ok(ApiResponse<AuthTokenDto>.Ok(result, "Account created successfully."));
        }

        /// <summary>Login and receive JWT token</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            var result = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
            return Ok(ApiResponse<AuthTokenDto>.Ok(result));
        }

        /// <summary>Change current user password</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        {
            await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
            return Ok(ApiResponse.Ok("Password changed successfully."));
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UpTask.Application.Common.Interfaces;

namespace UpTask.API.Services;

/// <summary>
/// Resolves the authenticated user's identity from the HttpContext Claims Principal.
/// Registered as Scoped — safe to inject into Application handlers.
/// </summary>
internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(_user?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : Guid.Empty;

    public string Email =>
        _user?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

    public string Role =>
        _user?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        _user?.Identity?.IsAuthenticated ?? false;
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UpTask.Application.Common.Interfaces;

namespace UpTask.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return value is not null && Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email)
                         ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}

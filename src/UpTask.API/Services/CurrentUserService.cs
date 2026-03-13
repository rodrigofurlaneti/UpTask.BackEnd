using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UpTask.Application.Common.Interfaces;
namespace UpTask.API.Services;
internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    public Guid UserId
    {
        get
        {
            var idClaim = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                          ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
        }
    }
    public string Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue(ClaimTypes.Email)
        ?? string.Empty;
    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}
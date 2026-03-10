using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UpTask.Application.Common.Interfaces;

namespace UpTask.Infrastructure.Authentication;

public sealed class JwtService(IConfiguration config) : IJwtService
{
    private readonly string _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured.");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "UpTask";
    private readonly string _audience = config["Jwt:Audience"] ?? "UpTask";
    private readonly int _expiresInMinutes = int.Parse(config["Jwt:ExpiresInMinutes"] ?? "60");

    public string GenerateToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid UserId, string Email, string Role)? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)!;
            var role = principal.FindFirstValue(ClaimTypes.Role)!;
            return (userId, email, role);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

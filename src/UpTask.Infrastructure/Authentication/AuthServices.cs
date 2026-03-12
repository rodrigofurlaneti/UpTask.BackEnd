using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UpTask.Application.Common.Interfaces;

namespace UpTask.Infrastructure.Authentication;

/// <summary>Configuration options for JWT generation.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "UpTask.API";
    public string Audience { get; init; } = "UpTask.Client";
    public int ExpirationMinutes { get; init; } = 60;
}

/// <summary>Generates and validates JWT Bearer tokens using HMAC-SHA256.</summary>
internal sealed class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token, out Guid userId)
    {
        userId = Guid.Empty;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;
            userId = Guid.Parse(jwt.Subject);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>BCrypt-based password hashing and verification.</summary>
internal sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string plainText) =>
        BCrypt.Net.BCrypt.HashPassword(plainText, WorkFactor);

    public bool Verify(string plainText, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainText, hash);
}

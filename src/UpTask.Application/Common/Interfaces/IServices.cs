namespace UpTask.Application.Common.Interfaces;

/// <summary>Hashes and verifies user passwords.</summary>
public interface IPasswordService
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}

/// <summary>Issues and validates JWT Bearer tokens.</summary>
public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string role);
    bool ValidateToken(string token, out Guid userId);
}

/// <summary>Returns the authenticated user context from the HTTP request.</summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}

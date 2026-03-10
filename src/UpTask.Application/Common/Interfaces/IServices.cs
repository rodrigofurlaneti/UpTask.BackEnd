namespace UpTask.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string role);
    (Guid UserId, string Email, string Role)? ValidateToken(string token);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendWelcomeAsync(string toEmail, string userName, CancellationToken ct = default);
}

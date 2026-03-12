namespace UpTask.Application.Features.Auth.DTOs
{
    // ── DTOs ─────────────────────────────────────────────────────────────────────
    public sealed record AuthTokenDto(
        string AccessToken,
        string TokenType,
        int ExpiresInSeconds,
        Guid UserId,
        string Email,
        string Role,
        string Name);
}

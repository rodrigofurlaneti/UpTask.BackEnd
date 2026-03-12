using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;

namespace UpTask.Domain.Entities;

/// <summary>
/// User aggregate root.
/// Encapsulates all identity and profile rules.
/// </summary>
public sealed class User : Entity
{
    // ── State ─────────────────────────────────────────────────────────────────
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserProfile Profile { get; private set; } = UserProfile.Member;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string? AvatarUrl { get; private set; }
    public string? Phone { get; private set; }
    public string TimeZone { get; private set; } = "America/Sao_Paulo";
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<ProjectMember> _projectMemberships = [];
    public IReadOnlyCollection<ProjectMember> ProjectMemberships => _projectMemberships.AsReadOnly();

    // ── Constructor ───────────────────────────────────────────────────────────
    private User() { } // EF Core

    // ── Factory ───────────────────────────────────────────────────────────────
    public static User Create(string name, Email email, string passwordHash,
        UserProfile profile = UserProfile.Member)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        if (name.Trim().Length < 2)
            throw new DomainException("Name must have at least 2 characters.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Profile = profile,
            Status = UserStatus.Active,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, user.Email.Value));
        return user;
    }

    // ── Behavior ──────────────────────────────────────────────────────────────
    public void UpdateProfile(string name, string? phone, string? avatarUrl, string timeZone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        Name = name.Trim();
        Phone = phone?.Trim();
        AvatarUrl = avatarUrl;
        TimeZone = string.IsNullOrWhiteSpace(timeZone) ? "America/Sao_Paulo" : timeZone;
        Touch();
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Token cannot be empty.");

        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash is required.");

        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        Touch();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        Touch();
    }

    public void Suspend()
    {
        if (Status == UserStatus.Suspended)
            throw new DomainException("User is already suspended.");

        Status = UserStatus.Suspended;
        Touch();
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        Touch();
    }

    // ── Domain Queries ────────────────────────────────────────────────────────
    public bool IsActive() => Status == UserStatus.Active;
    public bool IsAdmin() => Profile == UserProfile.Admin;

    public bool HasValidResetToken(string token) =>
        PasswordResetToken == token &&
        PasswordResetTokenExpiresAt.HasValue &&
        PasswordResetTokenExpiresAt.Value > DateTime.Now;
}

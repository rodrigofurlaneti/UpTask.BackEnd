using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;

namespace UpTask.Domain.Entities;

public sealed class User : BaseEntity
{
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

    // Navigation
    public UserSettings? Settings { get; private set; }
    private readonly List<ProjectMember> _projectMemberships = [];
    public IReadOnlyCollection<ProjectMember> ProjectMemberships => _projectMemberships.AsReadOnly();

    private User() { }

    public static User Create(string name, Email email, string passwordHash, UserProfile profile = UserProfile.Member)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Name is required.");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Profile = profile,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.Settings = UserSettings.CreateDefault(user.Id);
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email.Value));
        return user;
    }

    public void UpdateProfile(string name, string? phone, string? avatarUrl, string timeZone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Name is required.");
        Name = name.Trim();
        Phone = phone?.Trim();
        AvatarUrl = avatarUrl;
        TimeZone = timeZone;
        SetUpdatedAt();
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        SetUpdatedAt();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        SetUpdatedAt();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Suspend()
    {
        if (Status == UserStatus.Suspended) throw new BusinessRuleException("User is already suspended.");
        Status = UserStatus.Suspended;
        SetUpdatedAt();
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        SetUpdatedAt();
    }

    public bool IsActive() => Status == UserStatus.Active;
}

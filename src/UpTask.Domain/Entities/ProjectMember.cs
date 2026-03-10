using UpTask.Domain.Common;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Entities;

public sealed class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; private set; }
    public Guid? InvitedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public Project? Project { get; private set; }

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid userId, MemberRole role, Guid? invitedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            InvitedBy = invitedBy,
            AcceptedAt = invitedBy == null ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Accept()
    {
        AcceptedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void ChangeRole(MemberRole newRole)
    {
        Role = newRole;
        SetUpdatedAt();
    }
}

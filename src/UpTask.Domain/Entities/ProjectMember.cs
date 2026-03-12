using UpTask.Domain.Common;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Entities;

public sealed class ProjectMember : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; private set; }
    public Guid? InvitedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public Project? Project { get; private set; }

    private ProjectMember() { } // EF Core

    internal static ProjectMember Create(Guid projectId, Guid userId, MemberRole role, Guid? invitedBy)
    {
        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            InvitedBy = invitedBy,
            AcceptedAt = invitedBy is null ? DateTime.Now : null, // owner auto-accepts
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    internal void ChangeRole(MemberRole newRole)
    {
        Role = newRole;
        Touch();
    }

    public void Accept()
    {
        AcceptedAt = DateTime.Now;
        Touch();
    }
}

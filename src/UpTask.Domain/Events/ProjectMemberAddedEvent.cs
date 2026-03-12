using UpTask.Domain.Common;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a member is added to a project.</summary>
    public sealed record ProjectMemberAddedEvent(
        Guid ProjectId,
        Guid UserId,
        MemberRole Role,
        Guid InvitedBy) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

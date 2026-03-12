using UpTask.Domain.Common;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a project status changes.</summary>
    public sealed record ProjectStatusChangedEvent(
        Guid ProjectId,
        ProjectStatus OldStatus,
        ProjectStatus NewStatus) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

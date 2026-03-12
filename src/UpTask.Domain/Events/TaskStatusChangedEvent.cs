using UpTask.Domain.Common;
using Ts = UpTask.Domain.Enums;
namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a task status changes (not to Completed).</summary>
    public sealed record TaskStatusChangedEvent(
        Guid TaskId,
        Ts.TaskStatus OldStatus,
        Ts.TaskStatus NewStatus) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

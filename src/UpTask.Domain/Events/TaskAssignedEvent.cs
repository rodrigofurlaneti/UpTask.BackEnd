using UpTask.Domain.Common;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a task is assigned to a user.</summary>
    public sealed record TaskAssignedEvent(
        Guid TaskId,
        Guid AssigneeId,
        Guid? PreviousAssigneeId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

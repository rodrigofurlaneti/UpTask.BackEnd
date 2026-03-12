using UpTask.Domain.Common;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a task is marked complete.</summary>
    public sealed record TaskCompletedEvent(
        Guid TaskId,
        Guid CompletedBy,
        Guid? ProjectId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

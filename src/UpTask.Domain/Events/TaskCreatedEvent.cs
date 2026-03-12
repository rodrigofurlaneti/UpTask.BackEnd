using UpTask.Domain.Common;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a new task is created.</summary>
    public sealed record TaskCreatedEvent(
        Guid TaskId,
        Guid CreatedBy,
        Guid? ProjectId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

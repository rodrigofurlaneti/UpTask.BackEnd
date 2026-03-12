using UpTask.Domain.Common;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a project is created.</summary>
    public sealed record ProjectCreatedEvent(
        Guid ProjectId,
        Guid OwnerId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

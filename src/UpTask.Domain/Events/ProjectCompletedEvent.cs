using UpTask.Domain.Common;
namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a project reaches 100% completion.</summary>
    public sealed record ProjectCompletedEvent(
        Guid ProjectId,
        DateTime CompletedAt) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

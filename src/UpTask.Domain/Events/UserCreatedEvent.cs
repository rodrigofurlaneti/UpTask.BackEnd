using UpTask.Domain.Common;

namespace UpTask.Domain.Events
{
    /// <summary>Domain event raised when a new user account is created.</summary>
    public sealed record UserCreatedEvent(
        Guid UserId,
        string Email) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
    }
}

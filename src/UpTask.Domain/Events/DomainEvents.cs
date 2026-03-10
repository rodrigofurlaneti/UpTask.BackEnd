using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;

namespace UpTask.Domain.Events;

public record UserCreatedEvent(Guid UserId, string Email) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProjectCreatedEvent(Guid ProjectId, Guid OwnerId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProjectCompletedEvent(Guid ProjectId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskCreatedEvent(Guid TaskId, Guid CreatedBy, Guid? ProjectId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskCompletedEvent(Guid TaskId, Guid CompletedBy, Guid? ProjectId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskAssignedEvent(Guid TaskId, Guid AssigneeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskStatusChangedEvent(Guid TaskId, TaskStatus OldStatus, TaskStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

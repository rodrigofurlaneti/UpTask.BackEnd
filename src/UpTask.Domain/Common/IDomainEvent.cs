namespace UpTask.Domain.Common;
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

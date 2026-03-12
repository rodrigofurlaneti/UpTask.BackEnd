namespace UpTask.Domain.Common;
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected init; }
    public DateTime CreatedAt { get; protected init; }
    public DateTime UpdatedAt { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Touch() => UpdatedAt = DateTime.UtcNow;
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

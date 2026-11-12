namespace kecas.domain.abstractions;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity()
    {
        // EF Core
    }

    public Guid Id { get; init; }

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => [.. _domainEvents];

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

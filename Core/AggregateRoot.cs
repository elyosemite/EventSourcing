namespace Core;

public abstract class AggregateRoot : IAggregateRoot, IEventSourcing
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }

    protected abstract void Dispatch(IDomainEvent @event);

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _domainEvents.AsReadOnly();
    
    public void MarkEventsAsCommitted() => _domainEvents.Clear();

    protected void EmitDomainEvent(IDomainEvent @event)
    {
        EnrichEvent(@event);
        Dispatch(@event);
    }
    
    public void Rehydrate(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            Dispatch(@event);
            Version++;
        }
    }

    private void EnrichEvent(IDomainEvent @event)
    {
        if (@event is DomainEvent domainEventevent)
        {
            domainEventevent.OccurredOn = DateTime.UtcNow;
        }

        _domainEvents.Add(@event);
        Version++;
    }
}

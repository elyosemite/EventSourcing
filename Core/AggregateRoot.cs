using System.Collections.Frozen;

namespace Core;

public abstract class DomainEventRegistry
{
    public abstract FrozenDictionary<Type, Action<IAggregateRoot, IDomainEvent>> GetHandlers();
}

public abstract class AggregateRoot : DomainEventRegistry, IAggregateRoot, IEventSourcing
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _domainEvents.AsReadOnly();
    
    public void MarkEventsAsCommitted() => _domainEvents.Clear();

    protected void EmitDomainEvent(IDomainEvent @event)
    {
        EnrichEvent(@event);
        Dispatch(@event);
    }

    // Domain Event Dispatcher

    public void Dispatch(IDomainEvent @event)
    {
        if (!GetHandlers().TryGetValue(@event.GetType(), out var handle))
            throw new InvalidOperationException($"No handler registered for event type {@event.GetType().Name}");

        handle(this, @event);
    }

    // Event Sourcing methods
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

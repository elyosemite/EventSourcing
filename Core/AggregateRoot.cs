using System.Collections.Concurrent;

namespace Core;

public abstract class AggregateRoot : IEntity
{
    private readonly ConcurrentDictionary<Type, Action<IDomainEvent>> _handlers = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Register<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IDomainEvent
    {
        _handlers.TryAdd(typeof(TEvent), @event => handler.When((TEvent)@event));
    }

    protected void Apply(IDomainEvent @event)
    {
        Dispatch(@event);
        Version++;
        _domainEvents.Add(@event);
    }

    protected void Rehydrate(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            Dispatch(@event);
            Version++;
        }
    }

    private void Dispatch(IDomainEvent @event)
    {
        if (_handlers.TryGetValue(@event.GetType(), out var handle))
        {
            handle(@event);
        }
    }
}
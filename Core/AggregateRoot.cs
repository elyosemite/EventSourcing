using Serilog;

namespace Core;

public interface IDispatcher
{
    void Dispatch(IDomainEvent @event);
}

public abstract class AggregateRoot : IAggregateRoot, IEventSourcing, IDispatcher
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private Guid _correlationId = Guid.Empty;

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }
    public int ExpectedVersion => Version - _domainEvents.Count;

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _domainEvents.AsReadOnly();

    public void MarkEventsAsCommitted()
    {
        _domainEvents.Clear();
        _correlationId = Guid.Empty;
    }

    protected void EmitDomainEvent(IDomainEvent @event)
    {
        EnrichEvent(@event);
        Log.ForContext(GetType()).Debug(
            "stream:{AggregateId} correlation:{CorrelationId} → {EventType} v{Version}",
            Id, _correlationId, @event.GetType().Name, Version);
        Dispatch(@event);
    }

    public void Rehydrate(IEnumerable<IDomainEvent> history)
    {
        var rehydrationId = Guid.NewGuid();
        foreach (var @event in history)
        {
            Log.ForContext(GetType()).Debug(
                "stream:{AggregateId} rehydration:{RehydrationId} → {EventType} v{Version}",
                @event.AggregateId, rehydrationId, @event.GetType().Name, Version + 1);
            Dispatch(@event);
            Version++;
        }
    }

    private void EnrichEvent(IDomainEvent @event)
    {
        if (@event is DomainEvent domainEvent)
        {
            domainEvent.OccurredOn = DateTime.UtcNow;

            if (_correlationId == Guid.Empty)
                _correlationId = Guid.NewGuid();

            domainEvent.CorrelationId = _correlationId;
        }

        _domainEvents.Add(@event);
        Version++;
    }

    public abstract void Dispatch(IDomainEvent @event);
}

namespace Core.Aggregates;

public interface IEventBus
{
    void SubscribeAsync<TEvent>(IEventListener<TEvent> handler)
        where TEvent : IDomainEvent;

    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
         where TEvent : IDomainEvent;
    
    Task PublishAsync(IDomainEvent @event, CancellationToken ct = default);
}

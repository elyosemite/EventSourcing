namespace Core.Aggregates;

public interface IPublisher
{
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}

public interface IEventBus : IPublisher
{
    void SubscribeAsync<TEvent>(IEventListener<TEvent> handler)
        where TEvent : IDomainEvent;

    [Obsolete("Use PublishAsync from IPublisher.")]
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

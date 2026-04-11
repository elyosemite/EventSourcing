namespace Core;

public interface IPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

public interface ISubscriber
{
    void SubscribeAsync<TEvent>(IEventListener<TEvent> handler)
        where TEvent : IDomainEvent;
}

public interface IEventBus : IPublisher, ISubscriber
{
}

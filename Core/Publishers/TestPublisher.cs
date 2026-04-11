namespace Core.Publishers;

public class TestPublisher : IPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        // Efetua operação de negócio e publica
        throw new NotImplementedException();
    }
}

public class TestSubscribe : ISubscriber
{
    public void SubscribeAsync<TEvent>(IEventListener<TEvent> handler) where TEvent : IDomainEvent
    {
        throw new NotImplementedException();
    }
}
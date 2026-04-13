using System.Collections.Concurrent;

namespace Core;

public interface IPublisher<in  TEvent>
    where  TEvent : IDomainEvent
{
    Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface ISubscriber<in TEvent>
    where TEvent : IDomainEvent
{
    Task SubscribeAsync(Action<IEventListener<TEvent>> handler);
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    void Subscribe<TEvent>(IEventListener<TEvent> listener)
        where TEvent : IDomainEvent;
}

public class EventBusInMemoryTest<TEvent> : IEventBus
    where TEvent : IDomainEvent
{
    private readonly ConcurrentDictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _subscriptions = new();
    
    public async Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            Console.WriteLine($"No handlers registered for event type {typeof(TEvent).Name}");
            return;
        }

        foreach (var handler in handlers)
        {
            await handler(@event, cancellationToken);
        }
    }

    public void Subscribe(IEventListener<TEvent> listener)
    {
        var type = typeof(TEvent);
        
        if (!_subscriptions.TryGetValue(type, out var handlers))
        {
            handlers = [];
            _subscriptions[type] = handlers;
        }
        
        handlers.Add((@event, ct) => listener.HandleAsync((TEvent)@event, ct));

        return Task.CompletedTask;
    }
}

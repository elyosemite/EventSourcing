using Microsoft.Extensions.DependencyInjection;

namespace Core;

public class EventBusInMemory : IEventBus
{
    private readonly Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _subscriptions = new();
    
#if DEBUG
    public Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> Subscriptions => _subscriptions;
#endif
    /// <summary>
    /// Registers an event handler for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="listener"></param>
    public Task SubscribeAsync<TEvent>(IEventListener<TEvent> listener)
        where TEvent : IDomainEvent
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

    /// <summary>
    /// Registers an event one or more handlers for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default
    ) where TEvent : IDomainEvent
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
}

public static class EventBusInMemoryExtensions
{
    public static void AddEventBusInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBusInMemory>();
    }
}

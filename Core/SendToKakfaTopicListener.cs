using Core.Aggregates.Order.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Aggregates;

// Retomar para a sessão do claude --resume dd3e0d1b-5cab-47bf-8e7f-921d58ad9077
public class SendToKakfaTopicListener : IEventListener<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Kafka] Order {@event.AggregateId} placed for customer {@event.CustomerId}. Shipping to {@event.ShippingAddress}.");
        return Task.CompletedTask;
    }
}

public static class SendToKafkaTopicListenerExtensions
{
    public static IServiceCollection AddSendToKafkaTopicListener(this IServiceCollection services)
        => services.AddTransient<IEventListener<OrderPlacedEvent>, SendToKakfaTopicListener>();
}

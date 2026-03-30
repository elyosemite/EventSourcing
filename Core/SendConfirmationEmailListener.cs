using Core.Aggregates.Order.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Aggregates;

public class SendConfirmationEmailListener : IEventListener<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Email] Order {@event.AggregateId} placed for customer {@event.CustomerId}. Shipping to {@event.ShippingAddress}.");
        return Task.CompletedTask;
    }
}

public static class SendConfirmationEmailListenerExtensions
{
      public static IServiceCollection AddSendConfirmationEmailListener(this IServiceCollection services)
          => services.AddTransient<IEventListener<OrderPlacedEvent>, SendConfirmationEmailListener>();
}

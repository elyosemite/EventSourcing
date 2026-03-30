using Core.Aggregates.Order.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Aggregates;

public class ProjectToReadModelListener : IEventListener<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ReadModel] Projecting order {@event.AggregateId} to read model.");
        return Task.CompletedTask;
    }
}

public static class ProjectToReadModelListenerExtensions
{
    public static IServiceCollection AddProjectToReadModelListener(this IServiceCollection services)
        => services.AddTransient<IEventListener<OrderPlacedEvent>, ProjectToReadModelListener>();
}
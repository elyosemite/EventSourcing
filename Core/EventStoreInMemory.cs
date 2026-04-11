using Microsoft.Extensions.DependencyInjection;

namespace Core;

public class EventStoreInMemory : IEventStore
{
    private readonly Dictionary<Guid, List<IDomainEvent>> _store = new();
    private readonly Lock _lock = new();

    public Task AppendEventsAsync(
        Guid streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_store.TryGetValue(streamId, out var existing))
            {
                existing = [];
                _store[streamId] = existing;
            }

            var currentVersion = existing.Count;

            if (currentVersion != expectedVersion)
              throw new ConcurrencyException(streamId, expectedVersion, currentVersion);

            existing.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IDomainEvent>> LoadAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var events = _store.TryGetValue(streamId, out var existing)
                ? existing.AsReadOnly()
                : (IReadOnlyList<IDomainEvent>)[];

            return Task.FromResult(events);
        }
    }
}

public static class EventStoreInMemoryExtensions
{
    public static void AddEventStoreInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IEventStore, EventStoreInMemory>();
    }
}

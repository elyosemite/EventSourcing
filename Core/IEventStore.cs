namespace Core.Aggregates;

public interface IEventStore
{
    /// <summary>
    /// Appends the given events to the event store for the specified aggregate ID.
    /// The expected version is used for optimistic concurrency control. If the current version of the aggregate does not match the expected version, an exception should be thrown to indicate a concurrency conflict.
    /// </summary>
    /// <param name="aggregateId">Aggregate Id</param>
    /// <param name="events">All domain events from that aggregate</param>
    /// <param name="expectedVersion"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all events for the specified aggregate ID from the event store.
    /// The events should be returned in the order they were appended, allowing
    /// the caller to reconstruct the aggregate's state by replaying the events.
    /// </summary>
    /// <param name="aggregateId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<IDomainEvent>> LoadAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}

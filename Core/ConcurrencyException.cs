namespace Core;

public sealed class ConcurrencyException(Guid aggregateId, int expected, int actual)
      : Exception($"Concurrency conflict on aggregate {aggregateId}: expected version {expected}, but found {actual}.");

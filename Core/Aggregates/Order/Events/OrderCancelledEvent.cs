namespace Core.Aggregates.Order.Events;

public record OrderCancelledEvent(
    Guid     AggregateId,
    string   Reason,
    string   CancelledBy
  ) : DomainEvent(AggregateId);
namespace Core.Aggregates.Order.Events;

record OrderDeliveredEvent(
    Guid     AggregateId,
    DateTime DeliveredAt
) : DomainEvent(AggregateId);

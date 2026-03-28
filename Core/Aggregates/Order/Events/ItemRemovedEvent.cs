namespace Core.Aggregates.Order.Events;

public record ItemRemovedEvent(
    Guid     AggregateId,
    Guid     ProductId
) : DomainEvent(AggregateId);
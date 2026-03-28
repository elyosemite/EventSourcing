namespace Core.Aggregates.Order.Events;

public record ItemAddedEvent(
    Guid     AggregateId,
    Guid     ProductId,
    string   ProductName,
    int      Quantity,
    decimal  UnitPrice
) : DomainEvent(AggregateId);


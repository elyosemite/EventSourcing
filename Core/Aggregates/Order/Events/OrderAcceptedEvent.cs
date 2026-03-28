namespace Core.Aggregates.Order.Events;

public record OrderAcceptedEvent(
    Guid OrderId,
    string AcceptedBy,
    decimal Amount) : DomainEvent(OrderId);
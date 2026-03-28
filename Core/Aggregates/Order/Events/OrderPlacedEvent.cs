namespace Core.Aggregates.Order.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    string CustomerId,
    string ShippingAddress) : DomainEvent(OrderId);
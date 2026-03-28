namespace Core.Aggregates.Order.Events;

public record ShipmentDispatchedEvent(
    Guid OrderId,
    string   TrackingCode,
    string   Carrier) : DomainEvent(OrderId);
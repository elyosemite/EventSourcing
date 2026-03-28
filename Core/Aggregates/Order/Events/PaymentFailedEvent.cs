namespace Core.Aggregates.Order.Events;

public record PaymentFailedEvent(
    Guid OrderId,
    string Reason,
    decimal Amount) : DomainEvent(OrderId);
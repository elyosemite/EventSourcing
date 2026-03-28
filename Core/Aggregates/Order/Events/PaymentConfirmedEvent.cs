namespace Core.Aggregates.Order.Events;

public record PaymentConfirmedEvent(
    Guid OrderId,
    string PaymentId,
    decimal Amount) : DomainEvent(OrderId);
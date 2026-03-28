namespace Core.Aggregates.Order.Events;

public record OrderRefundRequestedEvent(
    Guid     AggregateId,
    decimal  RefundAmount,
    string   Reason
) : DomainEvent(AggregateId);
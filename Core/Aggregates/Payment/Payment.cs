using System.Collections.Frozen;

namespace Core.Aggregates.Payment;

// Domain Events
public record PaymentCreated(Guid OrderId, decimal Amount, string Currency) : DomainEvent(OrderId);
public record PaymentAmountUpdated(Guid OrderId, decimal NewAmount) : DomainEvent(OrderId);
public record PaymentCurrencyUpdated(Guid OrderId, string NewCurrency) : DomainEvent(OrderId);

public class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    internal readonly FrozenDictionary<Type, Action<Payment, IDomainEvent>> _handlers = 
        DomainEventHandler.CreateFor<Payment>()
            .On<PaymentCreated>(static (aggregate, @event) => aggregate.Apply(@event))
            .On<PaymentAmountUpdated>(static (aggregate, @event) => aggregate.Apply(@event))
            .On<PaymentCurrencyUpdated>(static (aggregate, @event) => aggregate.Apply(@event))
            .BuildFrozen();

    private Payment() { }

    public override FrozenDictionary<Type, Action<Payment, IDomainEvent>> GetHandlers() => _handlers;

    public static Payment Create(Guid orderId, decimal amount, string currency)
    {
        var payment = new Payment();
        payment.EmitDomainEvent(new PaymentCreated(orderId, amount, currency));
        return payment;
    }

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(newAmount));
        EmitDomainEvent(new PaymentAmountUpdated(OrderId, newAmount));
    }

    public static Payment RestoreFromHistory(IEnumerable<IDomainEvent> history)
    {
        var payment = new Payment();
        payment.Rehydrate(history);
        return payment;
    }

    #region Event Handlers
    public void Apply(PaymentCreated @event)
    {
        Id = @event.OrderId;
        OrderId = @event.OrderId;
        Amount = @event.Amount;
        Currency = @event.Currency;
    }

    public void Apply(PaymentAmountUpdated @event)
    {
        Amount = @event.NewAmount;
    }

    public void Apply(PaymentCurrencyUpdated @event)
    {
        Currency = @event.NewCurrency;
    }
    #endregion
}

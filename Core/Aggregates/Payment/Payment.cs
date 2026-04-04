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

    private Payment() { }

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

    protected override void Dispatch(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaymentCreated e:
                Apply(e);
                break;
            case PaymentAmountUpdated e:
                Apply(e);
                break;
            case PaymentCurrencyUpdated e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"No handler for event type {@event.GetType().Name}");
        }
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

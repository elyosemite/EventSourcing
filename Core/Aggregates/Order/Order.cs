namespace Core.Aggregates.Order;

using Core.Aggregates.Order.Events;

public partial class Order
    : AggregateRoot,
    IEventHandler<OrderPlacedEvent>,
    IEventHandler<PaymentConfirmedEvent>
{
    public string CustomerId { get; private set; }
    public string ShippingAddress { get; private set; }
    
    public string PaymentId { get; private set; }
    public decimal Amount { get; private set; }

    private Order()
    {
        Register<OrderPlacedEvent>(this);
        Register<PaymentConfirmedEvent>(this);
    }

    public static Order Place(Guid id, string customerId, string shippingAddress)
    {
        var order = new Order();
        order.Apply(new OrderPlacedEvent(id, customerId, shippingAddress));
        return order;
    }

    public static Order RehydrateFromHistory(IEnumerable<IDomainEvent> history)
    {
        var order = new Order();
        order.Rehydrate(history);
        return order;
    }
    
    // ---- Comportamentos do sistema ----
    public void ConfirmPayment(string paymentId, decimal amount)
    {
        this.Apply(new PaymentConfirmedEvent(this.Id, PaymentId, amount));
    }
}

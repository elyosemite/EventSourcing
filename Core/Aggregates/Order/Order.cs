namespace Core.Aggregates.Order;

using Core.Aggregates.Order.Events;

public partial class Order : AggregateRoot
{
    public string CustomerId { get; private set; }
    public string ShippingAddress { get; private set; }
    public string PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; }

    private Order()
    {
        Register<OrderPlacedEvent>(this);
        Register<PaymentConfirmedEvent>(this);
        Register<PaymentFailedEvent>(this);
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
        if (paymentId == "FAIL")
        {
            Apply(new PaymentFailedEvent(Id, "Payment failed due to invalid payment ID.", amount));
            return;
        }
        Apply(new PaymentConfirmedEvent(Id, paymentId, amount));
    }
}

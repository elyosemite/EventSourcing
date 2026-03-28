using Core.Aggregates.Order.Events;

namespace Core.Aggregates.Order;

public partial class Order
{
    public void When(PaymentConfirmedEvent @event)
    {
        Id = @event.OrderId;    
        Amount = @event.Amount;
        PaymentId = @event.PaymentId;
    }
}
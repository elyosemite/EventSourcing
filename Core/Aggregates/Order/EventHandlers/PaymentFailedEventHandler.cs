using Core.Aggregates.Order.Events;

namespace Core.Aggregates.Order;

public partial class Order
{
    public void When(PaymentFailedEvent @event)
    {
        Reason = @event.Reason;
        Amount = @event.Amount;
    }
}
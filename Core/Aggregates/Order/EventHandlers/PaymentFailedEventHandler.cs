using Core.Aggregates.Order.Events;

namespace Core.Aggregates.Order;

public partial class Order : IEventHandler<PaymentFailedEvent>
{
    public void When(PaymentFailedEvent @event)
    {
        Reason = @event.Reason;
        Amount = @event.Amount;
    }
}
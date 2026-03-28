namespace Core.Aggregates.Order;

using Core.Aggregates.Order.Events;

public partial class Order
{
    public void When(OrderPlacedEvent @event)
    {
        Console.WriteLine($"Order placed: {@event.OrderId} for customer {@event.CustomerId}");
        Id = @event.AggregateId;
        CustomerId = @event.CustomerId;
        ShippingAddress = @event.ShippingAddress;
    }
}

namespace Sample.WebApi.Features;

using Core.Aggregates;
using Core.Aggregates.Order;

public record OrderItemDto(Guid ProductId, int Quantity);
public record PlaceOrderRequest(Guid CustomerId, string ShipmentAddress, List<OrderItemDto> Items);
public record PlaceOrderResponse(Guid OrderId);

// You have to create an instance for this class manually and call the Handle method to execute the command.
public class PlaceOrder(
    IEventStore eventStore,
    IEventBus eventBus
    )
{
    public async Task<PlaceOrderResponse> Handle(PlaceOrderRequest request)
    {
        var order = Order.Place(Guid.NewGuid(), request.CustomerId.ToString(), request.ShipmentAddress);

        var expectedversion = order.Version - order.DomainEvents.Count;

        await eventStore.AppendEventsAsync(order.Id, order.DomainEvents, expectedversion);

        foreach (var @event in order.DomainEvents)
        {
            await eventBus.PublishAsync(@event);
        }

        order.ClearDomainEvents();

        // Validate the aggregate invariants and business rules here if needed...

        return new PlaceOrderResponse(order.Id);
    }
}

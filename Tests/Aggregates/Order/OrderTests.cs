using Core.Aggregates.Order;
using Core.Aggregates.Order.Events;

namespace Tests.Aggregates;

[TestFixture]
public class OrderTests
{
    private static readonly Guid   OrderId         = Guid.NewGuid();
    private static readonly string CustomerId      = "customer-123";
    private static readonly string ShippingAddress = "Rua das Flores, 42";

    [Test]
    public void Place_SetsId()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.Id, Is.EqualTo(OrderId));
    }

    [Test]
    public void Place_SetsCustomerId()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.CustomerId, Is.EqualTo(CustomerId));
    }

    [Test]
    public void Place_SetsShippingAddress()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.ShippingAddress, Is.EqualTo(ShippingAddress));
    }

    [Test]
    public void Place_IncrementsVersionToOne()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.Version, Is.EqualTo(1));
    }

    [Test]
    public void Place_AccumulatesExactlyOneDomainEvent()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.DomainEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public void Place_AccumulatedEventIsOrderPlacedEvent()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        Assert.That(order.DomainEvents[0], Is.InstanceOf<OrderPlacedEvent>());
    }

    // ── ClearDomainEvents ────────────────────────────────────────

    [Test]
    public void ClearDomainEvents_EmptiesAccumulatedEvents()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        order.ClearDomainEvents();

        Assert.That(order.DomainEvents, Is.Empty);
    }

    [Test]
    public void ClearDomainEvents_DoesNotChangeVersion()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);

        order.ClearDomainEvents();

        Assert.That(order.Version, Is.EqualTo(1));
    }

    // ── Rehydrate ────────────────────────────────────────────────

    [Test]
    public void Rehydrate_RebuildsStateFromHistory()
    {
        var history = new[]
        {
            new OrderPlacedEvent(OrderId, CustomerId, ShippingAddress)
        };

        var order = Order.RehydrateFromHistory(history);

        Assert.That(order.Id,              Is.EqualTo(OrderId));
        Assert.That(order.CustomerId,      Is.EqualTo(CustomerId));
        Assert.That(order.ShippingAddress, Is.EqualTo(ShippingAddress));
    }

    [Test]
    public void Rehydrate_DoesNotAccumulateDomainEvents()
    {
        var history = new[]
        {
            new OrderPlacedEvent(OrderId, CustomerId, ShippingAddress)
        };

        var order = Order.RehydrateFromHistory(history);

        Assert.That(order.DomainEvents, Is.Empty);
    }

    [Test]
    public void Rehydrate_VersionMatchesNumberOfEventsReplayed()
    {
        var history = new[]
        {
            new OrderPlacedEvent(OrderId, CustomerId, ShippingAddress)
        };

        var order = Order.RehydrateFromHistory(history);

        Assert.That(order.Version, Is.EqualTo(history.Length));
    }

    [Test]
    public void ConfirmPayment_success()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);
        order.ConfirmPayment("payment-id-123", 123m);
        
        Assert.That(order.Version, Is.EqualTo(2));
        Assert.That(order.Amount, Is.EqualTo(123m));
    }

    [Test]
    public void ConfirmPayment_failure()
    {
        var order = Order.Place(OrderId, CustomerId, ShippingAddress);
        order.ConfirmPayment("FAIL", 123m);
        
        Assert.That(order.Version, Is.EqualTo(2));
        Assert.That(order.Amount, Is.EqualTo(123m));
        Assert.That(order.Reason, Is.EqualTo("Payment failed due to invalid payment ID."));
    }
}

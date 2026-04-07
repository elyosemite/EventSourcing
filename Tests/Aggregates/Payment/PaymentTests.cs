using Core;
using Core.Aggregates.Payment;

namespace Tests.Aggregates;

[TestFixture]
public class PaymentTests
{
    private static readonly Guid OrderId = Guid.NewGuid();

    [Test]
    public void Initiate_ShouldEmit_PaymentInitiated()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");

        Assert.That(payment.UncommittedEvents, Has.Count.EqualTo(1));
        Assert.That(payment.UncommittedEvents[0], Is.TypeOf<PaymentInitiated>());

        var @event = (PaymentInitiated)payment.UncommittedEvents[0];
        Assert.That(@event.Amount, Is.EqualTo(100m));
        Assert.That(@event.Currency, Is.EqualTo("BRL"));
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Pending));
    }

    [Test]
    public void ProcessCard_WhenSale_ShouldEmit_Authorized_And_Captured()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        var response = GatewayResponse.Sale("AUTH-001", "TXN-001");

        payment.ProcessCard("4111111111111111", "123", response);

        Assert.That(payment.UncommittedEvents, Has.Count.EqualTo(3)); // Initiated + Authorized + Captured
        Assert.That(payment.UncommittedEvents[1], Is.TypeOf<PaymentAuthorized>());
        Assert.That(payment.UncommittedEvents[2], Is.TypeOf<PaymentCaptured>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Captured));
    }

    [Test]
    public void ProcessCard_WhenAuthorizeOnly_ShouldEmit_Authorized()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        var response = GatewayResponse.Authorized("AUTH-001", "TXN-001");

        payment.ProcessCard("4111111111111111", "123", response);

        Assert.That(payment.UncommittedEvents, Has.Count.EqualTo(2)); // Initiated + Authorized
        Assert.That(payment.UncommittedEvents[1], Is.TypeOf<PaymentAuthorized>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Authorized));
    }

    [Test]
    public void ProcessCard_WhenDeclined_ShouldEmit_PaymentDeclined()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        var response = GatewayResponse.Declined("insufficient_funds", "Saldo insuficiente.");

        payment.ProcessCard("4111111111111111", "123", response);

        Assert.That(payment.UncommittedEvents[1], Is.TypeOf<PaymentDeclined>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Declined));
    }

    [Test]
    public void ProcessCard_WhenFailed_ShouldEmit_PaymentFailed()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        var response = GatewayResponse.Failed("gateway_timeout", "Gateway indisponível.");

        payment.ProcessCard("4111111111111111", "123", response);

        Assert.That(payment.UncommittedEvents[1], Is.TypeOf<PaymentFailed>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Failed));
    }

    [Test]
    public void Capture_AfterAuthorized_ShouldEmit_PaymentCaptured()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        payment.ProcessCard("4111111111111111", "123", GatewayResponse.Authorized("AUTH-001", "TXN-001"));

        payment.Capture();

        Assert.That(payment.UncommittedEvents.Last(), Is.TypeOf<PaymentCaptured>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Captured));
    }

    [Test]
    public void Cancel_BeforeCapture_ShouldEmit_PaymentCancelled()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");

        payment.Cancel("Cliente desistiu.", "customer");

        Assert.That(payment.UncommittedEvents.Last(), Is.TypeOf<PaymentCancelled>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Cancelled));
    }

    [Test]
    public void Cancel_AfterCaptured_ShouldThrow()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        payment.ProcessCard("4111111111111111", "123", GatewayResponse.Sale("AUTH-001", "TXN-001"));

        Assert.Throws<InvalidOperationException>(() => payment.Cancel("Tentativa inválida.", "customer"));
    }

    [Test]
    public void Refund_AfterCaptured_ShouldEmit_PaymentRefunded()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        payment.ProcessCard("4111111111111111", "123", GatewayResponse.Sale("AUTH-001", "TXN-001"));

        payment.Refund(100m, "Produto não entregue.");

        Assert.That(payment.UncommittedEvents.Last(), Is.TypeOf<PaymentRefunded>());
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public void RestoreFromHistory_ShouldReconstitute_State()
    {
        var history = new IDomainEvent[]
        {
            new PaymentInitiated(OrderId, 100m, "BRL"),
            new PaymentAuthorized(OrderId, "AUTH-001", "TXN-001"),
            new PaymentCaptured(OrderId, "TXN-001", 100m)
        };

        var payment = Payment.RestoreFromHistory(history);

        Assert.That(payment.Id, Is.EqualTo(OrderId));
        Assert.That(payment.Amount, Is.EqualTo(100m));
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Captured));
        Assert.That(payment.Version, Is.EqualTo(3));
        Assert.That(payment.UncommittedEvents, Is.Empty);
    }

    [Test]
    public void Events_InSameOperation_ShouldShare_CorrelationId()
    {
        var payment = Payment.Initiate(OrderId, 100m, "BRL");
        payment.ProcessCard("4111111111111111", "123", GatewayResponse.Sale("AUTH-001", "TXN-001"));

        var authorized = (PaymentAuthorized)payment.UncommittedEvents[1];
        var captured = (PaymentCaptured)payment.UncommittedEvents[2];

        Assert.That(authorized.CorrelationId, Is.EqualTo(captured.CorrelationId));
    }

    [Test]
    public void VerifyEventsLoading()
    {
        var history = new IDomainEvent[]
        {
            new PaymentInitiated(OrderId, 100m, "BRL"),
            new PaymentAuthorized(OrderId, "AUTH-001", "TXN-001")
        };
        
        var aggregate = Payment.RestoreFromHistory(history);
        Assert.That(aggregate.Id, Is.EqualTo(OrderId));
        Assert.That(aggregate.Amount, Is.EqualTo(100m));
        Assert.That(aggregate.Status, Is.EqualTo(PaymentStatus.Authorized));
        Assert.That(aggregate.Version, Is.EqualTo(2));
        Assert.That(aggregate.UncommittedEvents, Is.Empty);
        
        aggregate.Capture();
        Assert.That(aggregate.UncommittedEvents.Last(), Is.TypeOf<PaymentCaptured>());
        Assert.That(aggregate.Version, Is.EqualTo(3));
        Assert.That(aggregate.UncommittedEvents.Count, Is.EqualTo(1));
    }
}

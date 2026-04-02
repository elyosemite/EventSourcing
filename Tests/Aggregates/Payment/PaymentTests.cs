using Core;
using Core.Aggregates.Payment;

namespace Tests.Aggregates;

[TestFixture]
public class PaymentTests
{
    [Test]
    public void ConfirmPayment()
    {
        var payment = Payment.Create(Guid.NewGuid(), 100m, "USD");

        Assert.That(payment.UncommittedEvents, Has.Count.EqualTo(1));
        Assert.That(payment.UncommittedEvents[0], Is.TypeOf<PaymentCreated>());
        var createdEvent = (PaymentCreated)payment.UncommittedEvents[0];
        Assert.That(createdEvent.Amount, Is.EqualTo(100m));
        Assert.That(createdEvent.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void RestorePaymentFromHistory()
    {
        var orderId = Guid.NewGuid();
        var history = new IDomainEvent[]
        {
            new PaymentCreated(orderId, 100m, "USD"),
            new PaymentAmountUpdated(orderId, 150m),
            new PaymentCurrencyUpdated(orderId, "EUR")
        };

        var payment = Payment.RestoreFromHistory(history);

        Assert.That(payment.Id, Is.EqualTo(orderId));
        Assert.That(payment.Amount, Is.EqualTo(150m));
        Assert.That(payment.Currency, Is.EqualTo("EUR"));
        Assert.That(payment.Version, Is.EqualTo(3));
        Assert.That(payment.UncommittedEvents, Is.Empty);

        // update the amount with method
        payment.UpdateAmount(200m);
        Assert.That(payment.Amount, Is.EqualTo(200m));
        Assert.That(payment.UncommittedEvents, Has.Count.EqualTo(1));
        Assert.That(payment.UncommittedEvents[0], Is.TypeOf<PaymentAmountUpdated>());
        var amountUpdatedEvent = (PaymentAmountUpdated)payment.UncommittedEvents[0];
        Assert.That(amountUpdatedEvent.NewAmount, Is.EqualTo(200m));
    }
}

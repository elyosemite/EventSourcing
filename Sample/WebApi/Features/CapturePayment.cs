using Core.Aggregates;
using Core.Aggregates.Payment;

namespace EventSourcing.WebApi.Features;

public record CapturePaymentCommand(Guid PaymentId);

public class CapturePayment(IEventStore eventStore)
{
    public async Task HandleAsync(
        CapturePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var eventHistory = await eventStore.LoadAsync(request.PaymentId, cancellationToken);
        
        if (eventHistory.Count == 0)
            throw new KeyNotFoundException($"Payment {request.PaymentId} not found.");
        
        var payment = Payment.RestoreFromHistory(eventHistory);

        payment.Capture();

        await eventStore.AppendEventsAsync(
            payment.OrderId,
            payment.UncommittedEvents,
            payment.ExpectedVersion,
            cancellationToken
        );

        payment.MarkEventsAsCommitted();
    }
}

using Core.Aggregates;
using Core.Aggregates.Payment;

namespace EventSourcing.WebApi.Features;

public record Command(Guid OrderId,
    decimal Amount,
    string Currency,
    string CreditCardNumber,
    string CreditCardCode);

public class InitPayment(IEventStore eventStore)
{
    public async Task<Guid> HandleAsync(
        Command request,
        CancellationToken cancellationToken)
    {
        var payment = Payment.Initiate(request.OrderId, request.Amount, request.Currency);
        
        var expectedVersion = payment.Version - payment.UncommittedEvents.Count;
        
        await eventStore.AppendEventsAsync(
            payment.Id,
            payment.UncommittedEvents,
            expectedVersion,
            cancellationToken);
        
        payment.MarkEventsAsCommitted();
        
        return payment.Id;
    }
}

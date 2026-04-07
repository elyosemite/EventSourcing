using Core.Aggregates;
using Core.Aggregates.Payment;

namespace EventSourcing.WebApi.Features;

public record AuthorizePaymentCommand(Guid PaymentId, string CreditCardNumber, string CreditCardCode);

public record AuthorizePaymentBody(string CreditCardNumber, string CreditCardCode);

public record AuthorizePaymentResult(
    string Status,
    string? AuthorizationCode = null,
    string? DeclineCode = null,
    string? ErrorCode = null,
    string? Reason = null);

public class AuthorizePayment(IEventStore eventStore)
{
    public async Task<AuthorizePaymentResult> HandleAsync(
        AuthorizePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var history = await eventStore.LoadAsync(request.PaymentId, cancellationToken);

        if (history.Count == 0)
            throw new KeyNotFoundException($"Payment {request.PaymentId} not found.");

        var payment = Payment.RestoreFromHistory(history);

        // simulação — em produção seria uma chamada real ao gateway
        var gatewayResponse = GatewayResponse.Authorized(
            authCode: Guid.NewGuid().ToString(),
            transactionId: Guid.NewGuid().ToString());

        payment.ProcessCard(request.CreditCardNumber, request.CreditCardCode, gatewayResponse);

        await eventStore.AppendEventsAsync(
            payment.Id,
            payment.UncommittedEvents,
            payment.Version - payment.UncommittedEvents.Count,
            cancellationToken);

        payment.MarkEventsAsCommitted();

        return payment.Status switch
        {
            PaymentStatus.Authorized => new AuthorizePaymentResult(
                "Authorized",
                AuthorizationCode: payment.AuthorizationCode),

            PaymentStatus.Declined => new AuthorizePaymentResult(
                "Declined",
                DeclineCode: payment.DeclineCode,
                Reason: payment.DeclineReason),

            PaymentStatus.Failed => new AuthorizePaymentResult(
                "Failed",
                ErrorCode: payment.FailureErrorCode,
                Reason: payment.FailureReason),

            _ => throw new InvalidOperationException($"Status inesperado após ProcessCard: {payment.Status}")
        };
    }
}

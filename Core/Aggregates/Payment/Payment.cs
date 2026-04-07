namespace Core.Aggregates.Payment;

// Domain Events
public record PaymentInitiated(Guid OrderId, decimal Amount, string Currency) : DomainEvent(OrderId);
public record PaymentAuthorized(Guid OrderId, string AuthorizationCode, string GatewayTransactionId) : DomainEvent(OrderId);
public record PaymentCaptured(Guid OrderId, string GatewayTransactionId, decimal CapturedAmount) : DomainEvent(OrderId);
public record PaymentDeclined(Guid OrderId, string Code, string Reason) : DomainEvent(OrderId);
public record PaymentFailed(Guid OrderId, string ErrorCode, string Reason) : DomainEvent(OrderId);
public record PaymentCancelled(Guid OrderId, string Reason, string CancelledBy) : DomainEvent(OrderId);
public record PaymentRefunded(Guid OrderId, Guid RefundId, decimal Amount, string Reason) : DomainEvent(OrderId);
public record ChargebackInitiated(Guid OrderId, string DisputeId, string Reason) : DomainEvent(OrderId);
public record ChargebackResolved(Guid OrderId, string DisputeId, string Resolution) : DomainEvent(OrderId);
public record PaymentExpired(Guid OrderId) : DomainEvent(OrderId);

// Value Objects
public record CreditCard(string Number, string Code);

public record GatewayResponse
{
    public bool IsDeclined { get; init; }
    public bool IsFailed { get; init; }
    public bool IsSale { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? TransactionId { get; init; }
    public string? Code { get; init; }
    public string? Reason { get; init; }
    public string? ErrorCode { get; init; }

    public static GatewayResponse Authorized(string authCode, string transactionId) =>
        new() { AuthorizationCode = authCode, TransactionId = transactionId };

    public static GatewayResponse Sale(string authCode, string transactionId) =>
        new() { IsSale = true, AuthorizationCode = authCode, TransactionId = transactionId };

    public static GatewayResponse Declined(string code, string reason) =>
        new() { IsDeclined = true, Code = code, Reason = reason };

    public static GatewayResponse Failed(string errorCode, string reason) =>
        new() { IsFailed = true, ErrorCode = errorCode, Reason = reason };
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Declined,
    Failed,
    Cancelled,
    Refunded,
    Expired,
    ChargebackInProgress,
    ChargebackResolved
}

public class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public CreditCard? CreditCard { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? DeclineCode { get; private set; }
    public string? DeclineReason { get; private set; }
    public string? FailureErrorCode { get; private set; }
    public string? FailureReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public Guid? RefundId { get; private set; }
    public decimal? RefundedAmount { get; private set; }
    public string? RefundReason { get; private set; }
    public string? DisputeId { get; private set; }
    public string? ChargebackReason { get; private set; }
    public string? ChargebackResolution { get; private set; }

    private Payment() { }

    public static Payment Initiate(Guid orderId, decimal amount, string currency)
    {
        var payment = new Payment();
        payment.EmitDomainEvent(new PaymentInitiated(orderId, amount, currency));
        return payment;
    }

    public void ProcessCard(string number, string code, GatewayResponse response)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot process card in status {Status}.");

        if (response.IsFailed)
        {
            EmitDomainEvent(new PaymentFailed(OrderId, response.ErrorCode!, response.Reason!));
            return;
        }

        if (response.IsDeclined)
        {
            EmitDomainEvent(new PaymentDeclined(OrderId, response.Code!, response.Reason!));
            return;
        }

        EmitDomainEvent(new PaymentAuthorized(OrderId, response.AuthorizationCode!, response.TransactionId!));

        if (response.IsSale)
            EmitDomainEvent(new PaymentCaptured(OrderId, response.TransactionId!, Amount));
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot capture payment in status {Status}.");

        EmitDomainEvent(new PaymentCaptured(OrderId, GatewayTransactionId!, Amount));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded)
            throw new InvalidOperationException($"Cannot cancel payment in status {Status}.");

        EmitDomainEvent(new PaymentCancelled(OrderId, reason, cancelledBy));
    }

    public void Refund(decimal amount, string reason)
    {
        if (Status != PaymentStatus.Captured)
            throw new InvalidOperationException($"Cannot refund payment in status {Status}.");

        if (amount <= 0 || amount > Amount)
            throw new ArgumentException($"Refund amount must be between 0 and {Amount}.", nameof(amount));

        EmitDomainEvent(new PaymentRefunded(OrderId, Guid.NewGuid(), amount, reason));
    }

    public void InitiateChargeback(string disputeId, string reason)
    {
        if (Status != PaymentStatus.Captured)
            throw new InvalidOperationException($"Cannot initiate chargeback in status {Status}.");

        EmitDomainEvent(new ChargebackInitiated(OrderId, disputeId, reason));
    }

    public void ResolveChargeback(string disputeId, string resolution)
    {
        if (Status != PaymentStatus.ChargebackInProgress)
            throw new InvalidOperationException($"Cannot resolve chargeback in status {Status}.");

        EmitDomainEvent(new ChargebackResolved(OrderId, disputeId, resolution));
    }

    public void Expire()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot expire payment in status {Status}.");

        EmitDomainEvent(new PaymentExpired(OrderId));
    }

    public static Payment RestoreFromHistory(IEnumerable<IDomainEvent> history)
    {
        var payment = new Payment();
        payment.Rehydrate(history);
        return payment;
    }

    public override void Dispatch(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaymentInitiated e:    Apply(e); break;
            case PaymentAuthorized e:   Apply(e); break;
            case PaymentCaptured e:     Apply(e); break;
            case PaymentDeclined e:     Apply(e); break;
            case PaymentFailed e:       Apply(e); break;
            case PaymentCancelled e:    Apply(e); break;
            case PaymentRefunded e:     Apply(e); break;
            case ChargebackInitiated e: Apply(e); break;
            case ChargebackResolved e:  Apply(e); break;
            case PaymentExpired e:      Apply(e); break;
            default:
                throw new InvalidOperationException($"No handler for event type {@event.GetType().Name}");
        }
    }

    #region Apply
    private void Apply(PaymentInitiated @event)
    {
        Id = @event.OrderId;
        OrderId = @event.OrderId;
        Amount = @event.Amount;
        Currency = @event.Currency;
        Status = PaymentStatus.Pending;
    }

    private void Apply(PaymentAuthorized @event)
    {
        AuthorizationCode = @event.AuthorizationCode;
        GatewayTransactionId = @event.GatewayTransactionId;
        Status = PaymentStatus.Authorized;
    }

    private void Apply(PaymentCaptured @event)
    {
        GatewayTransactionId = @event.GatewayTransactionId;
        Status = PaymentStatus.Captured;
    }

    private void Apply(PaymentDeclined @event)
    {
        DeclineCode = @event.Code;
        DeclineReason = @event.Reason;
        Status = PaymentStatus.Declined;
    }

    private void Apply(PaymentFailed @event)
    {
        FailureErrorCode = @event.ErrorCode;
        FailureReason = @event.Reason;
        Status = PaymentStatus.Failed;
    }

    private void Apply(PaymentCancelled @event)
    {
        CancellationReason = @event.Reason;
        CancelledBy = @event.CancelledBy;
        Status = PaymentStatus.Cancelled;
    }

    private void Apply(PaymentRefunded @event)
    {
        RefundId = @event.RefundId;
        RefundedAmount = @event.Amount;
        RefundReason = @event.Reason;
        Status = PaymentStatus.Refunded;
    }

    private void Apply(ChargebackInitiated @event)
    {
        DisputeId = @event.DisputeId;
        ChargebackReason = @event.Reason;
        Status = PaymentStatus.ChargebackInProgress;
    }

    private void Apply(ChargebackResolved @event)
    {
        ChargebackResolution = @event.Resolution;
        Status = PaymentStatus.ChargebackResolved;
    }

    private void Apply(PaymentExpired @event) => Status = PaymentStatus.Expired;
    #endregion
}

# Order Domain Events

All events inherit from `DomainEvent`, which provides three base fields:

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the Order that emitted the event |
| `OccurredOn` | `DateTime` | UTC timestamp of when the event occurred (defaults to `DateTime.UtcNow`) |
| `EventVersion` | `int` | Schema version of the event (defaults to `1`) |

---

## Lifecycle Overview

```
OrderPlaced
    │
    ├── PaymentConfirmed ──► OrderAccepted ──► [ItemAdded / ItemRemoved] ──► ShipmentDispatched ──► OrderDelivered
    │                                                                                                      │
    │                                                                                               OrderRefundRequested
    └── PaymentFailed
    │
    └── OrderCancelled  (valid before ShipmentDispatched)
```

---

## Events

### 1. `OrderPlacedEvent`
Emitted when a customer places a new order.

| Field | Type | Description |
|---|---|---|
| `OrderId` | `Guid` | ID of the order (maps to `AggregateId`) |
| `CustomerId` | `string` | ID of the customer who placed the order |
| `ShippingAddress` | `string` | Delivery address provided at checkout |

---

### 2. `PaymentConfirmedEvent`
Emitted when payment for the order is successfully processed.

| Field | Type | Description |
|---|---|---|
| `OrderId` | `Guid` | ID of the order (maps to `AggregateId`) |
| `PaymentId` | `string` | External payment provider transaction ID |
| `Amount` | `decimal` | Total amount charged |

---

### 3. `PaymentFailedEvent`
Emitted when a payment attempt is rejected or fails.

| Field | Type | Description |
|---|---|---|
| `OrderId` | `Guid` | ID of the order (maps to `AggregateId`) |
| `Reason` | `string` | Description of why the payment failed |
| `Amount` | `decimal` | Amount that was attempted |

---

### 4. `OrderAcceptedEvent`
Emitted when the store accepts the order and begins preparation.

| Field | Type | Description |
|---|---|---|
| `OrderId` | `Guid` | ID of the order (maps to `AggregateId`) |
| `AcceptedBy` | `string` | Identifier of the operator or system that accepted the order |
| `Amount` | `decimal` | Confirmed total order amount |

---

### 5. `ItemAddedEvent`
Emitted when a product is added to the order during preparation.

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the order |
| `ProductId` | `Guid` | ID of the product being added |
| `ProductName` | `string` | Display name of the product |
| `Quantity` | `int` | Number of units added |
| `UnitPrice` | `decimal` | Price per unit at the time of adding |

---

### 6. `ItemRemovedEvent`
Emitted when a product is removed from the order.

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the order |
| `ProductId` | `Guid` | ID of the product being removed |

---

### 7. `ShipmentDispatchedEvent`
Emitted when the order leaves the warehouse and is handed to a carrier.

| Field | Type | Description |
|---|---|---|
| `OrderId` | `Guid` | ID of the order (maps to `AggregateId`) |
| `TrackingCode` | `string` | Carrier-issued tracking number |
| `Carrier` | `string` | Name of the shipping carrier (e.g. FedEx, DHL) |

---

### 8. `OrderDeliveredEvent`
Emitted when the carrier confirms successful delivery to the customer.

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the order |
| `DeliveredAt` | `DateTime` | UTC timestamp of the actual delivery |

---

### 9. `OrderCancelledEvent`
Emitted when an order is cancelled before it is dispatched.

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the order |
| `Reason` | `string` | Explanation for the cancellation |
| `CancelledBy` | `string` | Identifier of who triggered the cancellation (customer, operator, system) |

---

### 10. `OrderRefundRequestedEvent`
Emitted after delivery when a customer requests a refund.

| Field | Type | Description |
|---|---|---|
| `AggregateId` | `Guid` | ID of the order |
| `RefundAmount` | `decimal` | Amount requested for refund |
| `Reason` | `string` | Customer-provided reason for the refund request |

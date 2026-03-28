# EventSourcing

A learning project exploring **CQRS**, **Event Sourcing**, and **Domain-Driven Design (DDD)** patterns in .NET 10.

## Concepts

**Event Sourcing** — the state of an aggregate is never stored directly. Instead, a sequence of domain events is persisted, and the current state is rebuilt by replaying those events.

**CQRS** — Commands mutate state and produce events. Queries read a projection of that state. The write and read models are kept separate.

**DDD** — the domain is modeled around Aggregates, Entities, and Value Objects. Business rules live inside the aggregate, which enforces invariants before emitting events.

## Project Structure

```
EventSourcing/
├── Core/                         # Domain and infrastructure primitives
│   ├── IDomainEvent.cs           # Event contract
│   ├── DomainEvent.cs            # Abstract base record (AggregateId, OccurredOn, EventVersion)
│   ├── IEntity.cs
│   ├── IAggregateRoot.cs
│   ├── IEventHandler.cs          # IEventHandler<TEvent> — per-event handler contract
│   ├── AggregateRoot.cs          # Base class: Apply, Rehydrate, Dispatch
│   └── Aggregates/
│       └── Order/
│           ├── Order.cs          # Aggregate root — declares handlers, factory methods
│           ├── OrderStatus.cs
│           ├── Events/           # One record per domain event
│           └── EventHandlers/    # Partial classes: one When(TEvent) per file
│
└── Tests/
    └── Aggregates/
        └── Order/
            └── OrderTests.cs     # Unit tests for the Order aggregate
```

## How It Works

### 1. Defining a domain event

```csharp
public record OrderPlacedEvent(Guid AggregateId, string CustomerId, string ShippingAddress)
    : DomainEvent(AggregateId);
```

### 2. Implementing an aggregate

```csharp
public partial class Order : AggregateRoot, IEventHandler<OrderPlacedEvent>
{
    public string CustomerId { get; private set; }

    private Order()
    {
        Register<OrderPlacedEvent>(this); // no reflection — delegate registered once
    }

    public static Order Place(Guid id, string customerId, string address)
    {
        var order = new Order();
        order.Apply(new OrderPlacedEvent(id, customerId, address));
        return order;
    }

    public static Order RehydrateFromHistory(IEnumerable<IDomainEvent> history)
    {
        var order = new Order();
        order.Rehydrate(history);
        return order;
    }
}
```

### 3. Handling the event (partial class, isolated file)

```csharp
// Order.OrderPlaced.cs
public partial class Order
{
    public void When(OrderPlacedEvent @event)
    {
        Id         = @event.AggregateId;
        CustomerId = @event.CustomerId;
    }
}
```

### 4. Rebuilding state from history

```csharp
var history = eventStore.Load(orderId);         // fetch persisted events
var order   = Order.RehydrateFromHistory(history); // replay → current state
```

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Delegate dictionary over reflection | Handlers are registered once in the constructor; dispatch is a dictionary lookup |
| `IEventHandler<TEvent>` interface | The compiler enforces that a handler exists before `Register<T>` compiles |
| Partial classes for handlers | Each event handler lives in its own file — aggregates with many events stay organized |
| `DomainEvent` abstract record | Eliminates boilerplate (`AggregateId`, `OccurredOn`, `EventVersion`) from every event |
| `Apply` vs `Rehydrate` | `Apply` mutates state **and** accumulates events for publishing; `Rehydrate` only mutates state |

## Running the Tests

```bash
dotnet test Tests/Tests.csproj
```

## Planned

- [ ] `IEventBus` — local in-memory implementation
- [ ] `IEventStore` — in-memory, then persistent
- [ ] RabbitMQ event bus adapter
- [ ] Read model / projections

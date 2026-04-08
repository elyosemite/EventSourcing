# Event Bus e DI

| Campo   | Valor                        |
|---------|------------------------------|
| Autor   | Codex                        |
| Status  | Draft                        |
| Versão  | 0.1.0                        |
| Criado  | 2026-04-07                   |

---

## Objetivo

Definir um estilo simples de configuração de DI para registro de listeners e handlers de eventos sem:

- assembly scanning
- reflection manual
- auto-discovery implícita

O objetivo é manter o registro explícito, legível e fácil de evoluir.

---

## Princípios

- O `EventStore` cuida de concorrência otimista e `ExpectedVersion`
- O `EventBus` publica eventos já persistidos
- Listeners de domínio reagem a `IDomainEvent`
- Eventos de integração devem usar um bus separado
- O registro no DI deve ser explícito e centralizado

---

## Motivação

Hoje, quando há muitos listeners, é comum cair em duas opções ruins:

1. registrar tudo manualmente em muitos lugares do `Program.cs`
2. usar scanning/reflection para descobrir automaticamente os listeners

A proposta deste documento é seguir um terceiro caminho:

- registro explícito
- sintaxe curta
- sem comportamento mágico
- fácil de depurar

---

## Estratégia proposta

Separar o problema em duas partes:

1. registrar dependências concretas no container
2. registrar as assinaturas no bus em um ponto central

Em vez de depender de descoberta automática, a aplicação define exatamente:

- qual evento é publicado
- quais listeners reagem a ele
- em que momento a assinatura acontece

---

## Contratos sugeridos

### 1. Domain Event Bus

```csharp
public interface IEventListener<TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface IEventBus
{
    void SubscribeAsync<TEvent>(IEventListener<TEvent> listener)
        where TEvent : IDomainEvent;

    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
```

### 2. Integration Event Bus

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    Guid CorrelationId { get; }
    DateTime OccurredOnUtc { get; }
}

public interface IIntegrationEventListener<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventBus
{
    void Subscribe<TEvent>(IIntegrationEventListener<TEvent> listener)
        where TEvent : IIntegrationEvent;

    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

---

## Registro explícito no DI

### Objetivo

Permitir algo assim no `Program.cs`:

```csharp
services.AddIntegrationEventBus(events =>
{
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedKafkaListener>();
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedEmailListener>();
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedRabbitMqListener>();
});
```

Esse formato tem algumas vantagens:

- cada assinatura fica visível
- fica claro quais listeners respondem a cada evento
- não existe descoberta implícita
- a manutenção fica previsível

---

## Builder de registro

### Subscription

```csharp
public sealed record IntegrationSubscription(Action<IIntegrationEventBus> Register);
```

### Builder

```csharp
using Microsoft.Extensions.DependencyInjection;

public sealed class IntegrationEventSubscriptionsBuilder
{
    private readonly IServiceCollection _services;

    public IntegrationEventSubscriptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public IntegrationEventSubscriptionsBuilder AddListener<TEvent, TListener>()
        where TEvent : class, IIntegrationEvent
        where TListener : class, IIntegrationEventListener<TEvent>
    {
        _services.AddSingleton<TListener>();

        _services.AddSingleton<IntegrationSubscription>(sp =>
            new IntegrationSubscription(
                bus => bus.Subscribe(sp.GetRequiredService<TListener>())));

        return this;
    }
}
```

---

## Extensão de IServiceCollection

```csharp
using Microsoft.Extensions.DependencyInjection;

public static class IntegrationEventsServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationEventBus(
        this IServiceCollection services,
        Action<IntegrationEventSubscriptionsBuilder> configure)
    {
        services.AddSingleton<IIntegrationEventBus, IntegrationEventBusInMemory>();
        services.AddSingleton<IStartupFilter, IntegrationEventBusStartupFilter>();

        var builder = new IntegrationEventSubscriptionsBuilder(services);
        configure(builder);

        return services;
    }
}
```

---

## Ativação das assinaturas

As assinaturas podem ser ativadas no startup da aplicação.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public sealed class IntegrationEventBusStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var bus = app.ApplicationServices.GetRequiredService<IIntegrationEventBus>();
            var subscriptions = app.ApplicationServices.GetServices<IntegrationSubscription>();

            foreach (var subscription in subscriptions)
                subscription.Register(bus);

            next(app);
        };
    }
}
```

Esse passo garante que:

- os listeners foram resolvidos pelo container
- o bus recebeu todas as assinaturas uma única vez
- a composição ficou centralizada

---

## Exemplo concreto com evento de integração

### Evento

```csharp
public sealed record PaymentCapturedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredOnUtc,
    Guid PaymentId,
    string TransactionId,
    decimal Amount,
    string Currency
) : IIntegrationEvent;
```

### Listeners

```csharp
public sealed class PaymentCapturedKafkaListener
    : IIntegrationEventListener<PaymentCapturedIntegrationEvent>
{
    public Task HandleAsync(
        PaymentCapturedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Kafka] PaymentId={@event.PaymentId}");
        return Task.CompletedTask;
    }
}
```

```csharp
public sealed class PaymentCapturedEmailListener
    : IIntegrationEventListener<PaymentCapturedIntegrationEvent>
{
    public Task HandleAsync(
        PaymentCapturedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Email] PaymentId={@event.PaymentId}");
        return Task.CompletedTask;
    }
}
```

```csharp
public sealed class PaymentCapturedRabbitMqListener
    : IIntegrationEventListener<PaymentCapturedIntegrationEvent>
{
    public Task HandleAsync(
        PaymentCapturedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[RabbitMQ] PaymentId={@event.PaymentId}");
        return Task.CompletedTask;
    }
}
```

### Registro

```csharp
services.AddIntegrationEventBus(events =>
{
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedKafkaListener>();
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedEmailListener>();
    events.AddListener<PaymentCapturedIntegrationEvent, PaymentCapturedRabbitMqListener>();
});
```

### Publicação

```csharp
await integrationEventBus.PublishAsync(
    new PaymentCapturedIntegrationEvent(
        EventId: Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        OccurredOnUtc: DateTime.UtcNow,
        PaymentId: payment.Id,
        TransactionId: payment.GatewayTransactionId!,
        Amount: payment.Amount,
        Currency: payment.Currency),
    cancellationToken);
```

O resultado esperado é que os três listeners recebam o mesmo evento e reajam de forma independente.

---

## Como aplicar a mesma ideia ao Domain Event Bus

O mesmo estilo pode ser usado para `IEventBus`:

```csharp
services.AddDomainEventBus(events =>
{
    events.AddListener<PaymentCaptured, PaymentCapturedProjectionListener>();
    events.AddListener<PaymentCaptured, PaymentCapturedAuditListener>();
});
```

O importante é preservar a separação:

- `IDomainEvent` para reações internas
- `IIntegrationEvent` para contratos externos

---

## Responsabilidades

### EventStore

- persiste eventos
- valida `ExpectedVersion`
- detecta concorrência otimista

### EventBus

- publica eventos internos já aceitos
- notifica listeners do mesmo processo

### IntegrationEventBus

- publica contratos externos
- permite múltiplos consumidores para o mesmo evento
- desacopla transporte da lógica de domínio

### Listener

- reage a um evento específico
- executa uma responsabilidade pequena e isolada
- não deve recalcular `ExpectedVersion`

---

## Recomendações

- não publicar eventos de integração antes de persistir no `EventStore`
- não misturar `IDomainEvent` e `IIntegrationEvent` no mesmo bus
- não usar auto-discovery para esconder o grafo de dependências
- manter o registro centralizado em uma extensão de DI
- preferir um listener por responsabilidade

---

## Evoluções futuras

Os pontos abaixo podem ser adicionados depois, sem quebrar esse estilo de registro:

- `DomainEventEnvelope`
- `IntegrationEventEnvelope`
- metadata com `StreamVersion`
- outbox para publicação confiável
- retries e dead-letter para transporte externo

---

## Resumo

O estilo recomendado para este projeto é:

- registro explícito
- composição centralizada
- sem scanning
- sem reflection manual
- um builder fluente para reduzir ruído

Esse desenho mantém a configuração simples e previsível, sem sacrificar clareza arquitetural.

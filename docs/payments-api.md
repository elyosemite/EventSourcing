# Especificação — API de Pagamentos

| Campo   | Valor                        |
|---------|------------------------------|
| Autor   | Yuri Melo                    |
| Status  | Draft                        |
| Versão  | 0.2.0                        |
| Criado  | 2026-04-05                   |

---

## Ciclo de vida

```
[Iniciado] → [Autorizado] → [Capturado] ✓
                  ↓               ↓
             [Recusado]      [Reembolsado]
             [Falhou]              ↓
             [Cancelado]   [Estorno iniciado]
             [Expirado]           ↓
                          [Estorno resolvido]
```

| Evento | Quando ocorre |
|---|---|
| `PaymentInitiated` | Intenção de pagar registrada |
| `PaymentAuthorized` | Gateway pré-autorizou — dinheiro reservado no cartão |
| `PaymentCaptured` | Dinheiro efetivamente movido |
| `PaymentDeclined` | Gateway recusou (saldo, limite, dados inválidos) |
| `PaymentFailed` | Falha técnica (timeout, gateway fora do ar) |
| `PaymentCancelled` | Cancelado antes da captura |
| `PaymentRefunded` | Reembolso total ou parcial após captura |
| `ChargebackInitiated` | Cliente contestou a cobrança no banco |
| `ChargebackResolved` | Contestação encerrada (a favor ou contra o comerciante) |
| `PaymentExpired` | Sessão/link de pagamento expirou sem conclusão |

---

## Sumário

1. [Iniciar pagamento](#1-iniciar-pagamento)
2. [Autorizar pagamento](#2-autorizar-pagamento)
3. [Capturar pagamento](#3-capturar-pagamento)
4. [Cancelar pagamento](#4-cancelar-pagamento)
5. [Reembolsar pagamento](#5-reembolsar-pagamento)
6. [Iniciar contestação (chargeback)](#6-iniciar-contestação-chargeback)
7. [Resolver contestação (chargeback)](#7-resolver-contestação-chargeback)
8. [Expirar pagamento](#8-expirar-pagamento)
9. [Consultar estado do pagamento](#9-consultar-estado-do-pagamento)
10. [Consultar stream de eventos](#10-consultar-stream-de-eventos)

---

## 1. Iniciar pagamento

**Status:** Implementado
> ⚠️ A implementação atual combina as etapas 1 e 2 em um único endpoint `POST /payments`.

> Como cliente, quero registrar a intenção de pagamento informando o pedido, valor e moeda,
> para que o pagamento seja criado no sistema e aguarde a autorização do gateway.

### Endpoint

```
POST /payments
```

### Request

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 150.00,
  "currency": "BRL"
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `201 Created` | Pagamento iniciado com sucesso |
| `400 Bad Request` | Dados inválidos na requisição |

```json
// 201 Created
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

### Critérios de aceite

- [ ] Deve emitir `PaymentInitiated`
- [ ] Status resultante deve ser `Pending`
- [ ] Não deve processar cartão nesta etapa

---

## 2. Autorizar pagamento

**Status:** A implementar

> Como cliente, quero informar os dados do cartão para que o gateway processe a autorização,
> para que o valor seja reservado e o pagamento avance para a captura.

### Endpoint

```
POST /payments/{id}/authorize
```

### Request

```json
{
  "creditCardNumber": "4111111111111111",
  "creditCardCode": "123"
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Cartão processado — ver `status` no corpo para o resultado |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `Pending` |

```json
// 200 OK — autorizado
{ "status": "Authorized", "authorizationCode": "AUTH-001" }

// 200 OK — recusado
{ "status": "Declined", "declineCode": "insufficient_funds", "reason": "Saldo insuficiente." }

// 200 OK — falha técnica
{ "status": "Failed", "errorCode": "gateway_timeout", "reason": "Gateway indisponível." }
```

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve retornar `409` se o status não for `Pending`
- [ ] Se o gateway aprovar em modo *authorize only*, deve emitir `PaymentAuthorized`
- [ ] Se o gateway aprovar em modo *sale*, deve emitir `PaymentAuthorized` + `PaymentCaptured`
- [ ] Se o gateway recusar, deve emitir `PaymentDeclined`
- [ ] Se houver falha técnica, deve emitir `PaymentFailed`
- [ ] Todos os eventos do mesmo comando devem compartilhar o mesmo `CorrelationId`

---

## 3. Capturar pagamento

**Status:** A implementar

> Como operador financeiro, quero capturar um pagamento previamente autorizado,
> para que o valor seja efetivamente debitado do cliente após a confirmação do pedido.

### Endpoint

```
POST /payments/{id}/capture
```

### Request

Sem corpo.

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Pagamento capturado com sucesso |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `Authorized` |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `PaymentCaptured`
- [ ] Deve retornar `409` se o status não for `Authorized`

---

## 4. Cancelar pagamento

**Status:** A implementar

> Como cliente ou operador, quero cancelar um pagamento antes de sua captura,
> para que o valor reservado no cartão seja liberado.

### Endpoint

```
POST /payments/{id}/cancel
```

### Request

```json
{
  "reason": "Cliente desistiu da compra.",
  "cancelledBy": "customer"
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Pagamento cancelado |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento já capturado ou reembolsado |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `PaymentCancelled`
- [ ] Deve retornar `409` se o status for `Captured` ou `Refunded`
- [ ] O campo `cancelledBy` deve aceitar: `"customer"`, `"merchant"`, `"system"`

---

## 5. Reembolsar pagamento

**Status:** A implementar

> Como operador financeiro, quero reembolsar total ou parcialmente um pagamento capturado,
> para que o valor seja devolvido ao cliente em caso de devolução ou erro de cobrança.

### Endpoint

```
POST /payments/{id}/refund
```

### Request

```json
{
  "amount": 75.00,
  "reason": "Produto devolvido pelo cliente."
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Reembolso registrado |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `Captured` |
| `422 Unprocessable Entity` | Valor do reembolso inválido (zero, negativo ou maior que o original) |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `PaymentRefunded` com um `RefundId` gerado
- [ ] Deve aceitar reembolso parcial (valor menor que `Amount`)
- [ ] Deve rejeitar valor `<= 0` ou `> Amount`
- [ ] Deve retornar `409` se o status não for `Captured`

---

## 6. Iniciar contestação (chargeback)

**Status:** A implementar

> Como operador de risco, quero registrar uma contestação iniciada pelo banco do cliente,
> para que o pagamento entre em processo de disputa e seja bloqueado para novas operações.

### Endpoint

```
POST /payments/{id}/chargeback
```

### Request

```json
{
  "disputeId": "DSP-2026-00123",
  "reason": "fraud"
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Contestação registrada |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `Captured` |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `ChargebackInitiated`
- [ ] Deve retornar `409` se o status não for `Captured`
- [ ] O campo `reason` deve aceitar: `"fraud"`, `"not_received"`, `"duplicate"`, `"other"`

---

## 7. Resolver contestação (chargeback)

**Status:** A implementar

> Como operador de risco, quero registrar a resolução de uma contestação,
> para que o status do pagamento reflita o resultado da disputa.

### Endpoint

```
POST /payments/{id}/chargeback/resolve
```

### Request

```json
{
  "disputeId": "DSP-2026-00123",
  "resolution": "won_by_merchant"
}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Contestação resolvida |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `ChargebackInProgress` |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `ChargebackResolved`
- [ ] Deve retornar `409` se o status não for `ChargebackInProgress`
- [ ] O campo `resolution` deve aceitar: `"won_by_merchant"`, `"won_by_customer"`

---

## 8. Expirar pagamento

**Status:** A implementar

> Como sistema, quero marcar como expirado um pagamento pendente que ultrapassou o prazo,
> para que o slot do pedido seja liberado e nenhuma operação futura seja permitida.

### Endpoint

```
POST /payments/{id}/expire
```

### Request

Sem corpo.

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Pagamento expirado |
| `404 Not Found` | Pagamento não encontrado |
| `409 Conflict` | Pagamento não está no status `Pending` |

### Critérios de aceite

- [ ] Deve carregar o agregado via `RestoreFromHistory` antes de executar
- [ ] Deve emitir `PaymentExpired`
- [ ] Deve retornar `409` se o status não for `Pending`
- [ ] Tipicamente chamado por um job agendado, não por usuário final

---

## 9. Consultar estado do pagamento

**Status:** A implementar

> Como cliente ou operador, quero consultar o estado atual de um pagamento,
> para que eu possa verificar seu status, valor e informações de processamento.

### Endpoint

```
GET /payments/{id}
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Estado atual do pagamento |
| `404 Not Found` | Pagamento não encontrado |

```json
// 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 150.00,
  "currency": "BRL",
  "status": "Captured",
  "gatewayTransactionId": "TXN-001",
  "authorizationCode": "AUTH-001",
  "version": 3
}
```

### Critérios de aceite

- [ ] Deve reconstruir o estado via `RestoreFromHistory`
- [ ] Deve retornar `404` se não houver eventos para o `id` informado
- [ ] Não deve expor dados sensíveis do cartão

---

## 10. Consultar stream de eventos

**Status:** A implementar

> Como desenvolvedor ou operador de suporte, quero consultar todos os eventos de um pagamento em ordem cronológica,
> para que eu possa auditar o histórico completo de mudanças sem depender de logs externos.

### Endpoint

```
GET /payments/{id}/events
```

### Responses

| Status | Descrição |
|--------|-----------|
| `200 OK` | Lista de eventos em ordem de versão |
| `404 Not Found` | Nenhum evento encontrado para o `id` |

```json
// 200 OK
[
  {
    "type": "PaymentInitiated",
    "aggregateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "correlationId": "7c22...",
    "version": 1,
    "occurredOn": "2026-04-05T14:32:01Z",
    "data": { "amount": 150.00, "currency": "BRL" }
  },
  {
    "type": "PaymentAuthorized",
    "aggregateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "correlationId": "7c22...",
    "version": 2,
    "occurredOn": "2026-04-05T14:32:01Z",
    "data": { "authorizationCode": "AUTH-001", "gatewayTransactionId": "TXN-001" }
  },
  {
    "type": "PaymentCaptured",
    "aggregateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "correlationId": "7c22...",
    "version": 3,
    "occurredOn": "2026-04-05T14:32:01Z",
    "data": { "gatewayTransactionId": "TXN-001", "capturedAmount": 150.00 }
  }
]
```

### Critérios de aceite

- [ ] Deve retornar os eventos em ordem crescente de versão
- [ ] Deve incluir `type`, `aggregateId`, `correlationId`, `version`, `occurredOn` e `data`
- [ ] Deve retornar `404` se não houver eventos para o `id` informado
- [ ] Eventos do mesmo comando devem exibir o mesmo `correlationId`

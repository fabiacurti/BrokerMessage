 # 🏗️ Documentação da Arquitetura - Order Processing System

## 📐 Visão Geral da Arquitetura

Este documento descreve a arquitetura, padrões de design e decisões técnicas do Order Processing System.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CAMADA DE APRESENTAÇÃO                       │
│                                                                     │
│   HTTP Clients (cURL, Postman, Frontend)                           │
└────────────────────────────┬────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                    ASP.NET CORE WEB API                             │
│                    (Order API - Port 5000)                          │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Controllers                                                │   │
│  │  └─ OrdersController (REST Endpoints)                       │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                            │
│  ┌──────────────────────▼──────────────────────────────────────┐   │
│  │  Application Services                                       │   │
│  │  └─ OrderService (Business Logic)                           │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                            │
│  ┌──────────────────────▼──────────────────────────────────────┐   │
│  │  Data Access Layer                                          │   │
│  │  └─ InMemoryOrderRepository                                 │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
└────────────────────────────┬────────────────────────────────────────┘
                             │
        ┌────────────────────▼────────────────────┐
        │                                         │
┌───────▼──────────────────────────────┐  ┌──────▼──────────────────┐
│   RABITMQ MESSAGE BUS                │  │  IN-MEMORY DATA STORE   │
│                                      │  │                         │
│  Exchange: order-exchange (Topic)    │  │  ┌──────────────────┐   │
│                                      │  │  │ Orders (Dict)    │   │
│  ┌──────────────────────────────┐   │  │  └──────────────────┘   │
│  │ Routing Keys                 │   │  │                         │
│  ├──────────────────────────────┤   │  │  ┌──────────────────┐   │
│  │ • order.created              │   │  │  │ Products (Dict)  │   │
│  │ • payment.processed          │   │  │  │ • 000...001: 100 │   │
│  │ • inventory.reserved         │   │  │  │ • 000...002: 50  │   │
│  └──────────────────────────────┘   │  │  │ • 000...003: 75  │   │
└───────┬──────────────────────────────┘  └──────────────────────┘
        │
        │
        ├──────────────────────────┬──────────────────────┬─────────────┐
        │                          │                      │             │
┌───────▼────────────┐   ┌────────▼──────────┐  ┌───────▼────────┐   │
│  PAYMENT SERVICE   │   │ INVENTORY SERVICE │  │ NOTIFICATION   │   │
│  (Worker Service)  │   │ (Worker Service)  │  │ SERVICE        │   │
│                    │   │                   │  │ (Worker)       │   │
│ Queue:             │   │ Queue:            │  │                │   │
│ payment-queue      │   │ inventory-queue   │  │ Queues:        │   │
│                    │   │                   │  │ • noti-order   │   │
│ Consumes:          │   │ Consumes:         │  │ • noti-payment │   │
│ order.created      │   │ payment.processed │  │ • noti-inv     │   │
│                    │   │                   │  │                │   │
│ Publica:           │   │ Publica:          │  │ Escuta todos   │   │
│ payment.processed  │   │ inventory.        │  │ eventos        │   │
│                    │   │ reserved          │  │                │   │
└────────────────────┘   └───────────────────┘  └────────────────┘
```

## 🧩 Componentes

### 1. Shared Library (Biblioteca Compartilhada)

**Arquivo:** `src/Shared/`

**Responsabilidade:** Fornecer tipos e interfaces comuns

**Classes Principais:**

#### Events.cs
```csharp
IntegrationEvent (classe base)
├── OrderCreatedEvent
├── PaymentProcessedEvent
└── InventoryReservedEvent
```

**Características:**
- Todas as classes de evento herdam de `IntegrationEvent`
- Cada evento tem `EventId` (GUID único) e `CreatedAt` (timestamp)
- Contém todos os dados necessários para serem processados downstream
- Serializáveis em JSON

#### RabbitMqEventBus.cs

**Interfaces:**
```csharp
IEventPublisher
  └─ PublishAsync<T>(event, routingKey)

IEventConsumer
  └─ ConsumeAsync<T>(queueName, handler)
```

**Implementações:**
```csharp
RabbitMqEventPublisher
  ├─ Cria conexão com RabbitMQ
  ├─ Declara exchange (topic)
  ├─ Serializa eventos para JSON
  └─ Publica com confirmação

RabbitMqEventConsumer
  ├─ Conecta a uma fila
  ├─ Faz binding com routing key
  ├─ Desserializa mensagens
  ├─ Manual ACK
  └─ Retry automático em erro
```

### 2. Order API (ASP.NET Core Web API)

**Arquivo:** `src/OrderApi/`

**Porta:** 5000

**Responsabilidade:** Receber pedidos via HTTP e publicar eventos

**Estrutura de Camadas:**

```
Controllers (HTTP Interface)
    ↓
Services (Business Logic)
    ↓
Repositories (Data Access)
    ↓
In-Memory Store / RabbitMQ
```

#### Controllers/OrdersController.cs

**Endpoints:**

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| POST | `/api/orders` | Criar novo pedido | 201 Created |
| GET | `/api/orders/{id}` | Obter pedido | 200 OK |
| GET | `/api/orders` | Listar pedidos | 200 OK |
| GET | `/api/orders/health` | Health check | 200 OK |

**Fluxo:**
1. Cliente HTTP → POST /api/orders
2. Controller valida request
3. OrderService.CreateOrderAsync() é chamado
4. Pedido é salvo no repositório
5. Evento OrderCreatedEvent é publicado
6. Response com 201 Created é retornado

#### Services/OrderService.cs

**Responsabilidades:**
- Validar dados de entrada
- Calcular total do pedido
- Criar novo Order
- Persiste via repositório
- Publica evento via RabbitMQ

#### Repositories/OrderRepository.cs

**Implementação:** In-Memory (Dictionary thread-safe)

**Métodos:**
```csharp
CreateAsync(Order) → Order (com ID gerado)
GetByIdAsync(Guid) → Order ou KeyNotFoundException
UpdateAsync(Order) → Order (atualizado)
GetAllAsync() → List<Order>
```

**Thread-Safety:** Usa `lock` para sincronizar acessos

### 3. Payment Service (Worker Service)

**Arquivo:** `src/PaymentService/`

**Responsabilidade:** Processar pagamentos de pedidos criados

**Fluxo:**

```
RabbitMQ (payment-queue)
    ↓
PaymentProcessingWorker (Background Service)
    ↓
MockPaymentProcessor (simula processamento)
    ├─ 80% aprovação
    └─ 20% rejeição
    ↓
Publica PaymentProcessedEvent
    ↓
RabbitMQ (payment.processed routing key)
```

#### PaymentProcessingWorker.cs

**Características:**
- Implementa `BackgroundService`
- Escuta mensagens continuamente
- Processa uma por vez (QoS = 1)
- Manual ACK após sucesso
- Nack com retry em erro

**Fluxo de Mensagem:**
1. Worker recebe `OrderCreatedEvent`
2. Simula delay (500-2000ms)
3. Gera 80% chance de aprovação
4. Publica `PaymentProcessedEvent` com status
5. Reconhece mensagem original (ACK)

#### IPaymentProcessor.cs

**Implementação:** `MockPaymentProcessor`
- Simula processamento real
- Delay aleatório
- Taxa de aprovação configurável (80%)

### 4. Inventory Service (Worker Service)

**Arquivo:** `src/InventoryService/`

**Responsabilidade:** Reservar estoque para pedidos pagos

**Fluxo:**

```
RabbitMQ (inventory-queue)
    ↓
InventoryProcessingWorker
    ↓
Verifica pagamento = "approved"
    ↓
MockInventoryService (simula estoque)
    ├─ Valida disponibilidade
    └─ Decrementa estoque
    ↓
Publica InventoryReservedEvent
    ↓
RabbitMQ (inventory.reserved routing key)
```

#### Estoque Simulado

```csharp
Produto 000...001: 100 unidades
Produto 000...002: 50 unidades
Produto 000...003: 75 unidades
```

**Lógica de Reserva:**
1. Verifica se há quantidade disponível para todos itens
2. Se sim: decrementa estoque de cada item
3. Se não: publica status "insufficient"

### 5. Notification Service (Worker Service)

**Arquivo:** `src/NotificationService/`

**Responsabilidade:** Enviar notificações sobre eventos

**Fluxo:**

```
Múltiplas Filas em Paralelo:

Queue: notification-queue-order
    ↓ order.created
NotificationWorker (Task 1)
    ↓
Evento: OrderCreatedEvent
    ↓
MockNotificationSender
    ↓
[EMAIL] Obrigado pela compra!

Queue: notification-queue-payment
    ↓ payment.processed
NotificationWorker (Task 2)
    ↓
Evento: PaymentProcessedEvent
    ↓
MockNotificationSender
    ↓
[EMAIL] Pagamento aprovado/rejeitado

Queue: notification-queue-inventory
    ↓ inventory.reserved
NotificationWorker (Task 3)
    ↓
Evento: InventoryReservedEvent
    ↓
MockNotificationSender
    ↓
[EMAIL] Estoque reservado/insuficiente
```

**Implementação:**
- 3 Tasks simultâneas consumindo 3 filas diferentes
- Simula envio de email/SMS com logs
- Fornece feedback visual completo

## 🔄 Padrões de Design Utilizados

### 1. Publisher-Subscriber (Pub/Sub)

RabbitMQ Topic Exchange implementa padrão Pub/Sub:
- Publisher: Order API (publica order.created)
- Subscribers: Payment Service, Inventory Service, Notification Service

**Benefício:** Desacoplamento - A API não precisa conhecer os consumers

### 2. Event Sourcing (Simplificado)

Cada ação importante gera um evento:
- Order criado → `OrderCreatedEvent`
- Pagamento processado → `PaymentProcessedEvent`
- Estoque reservado → `InventoryReservedEvent`

**Benefício:** Histórico completo de transações

### 3. Saga Pattern (Simplificado)

Fluxo distribuído de múltiplas etapas:
```
Create Order
    ↓
Publish order.created
    ↓
[PAYMENT SERVICE] Process Payment
    ↓
Publish payment.processed
    ↓
[INVENTORY SERVICE] Reserve Inventory
    ↓
Publish inventory.reserved
    ↓
[NOTIFICATION SERVICE] Send Notifications
```

**Benefício:** Coordenação entre serviços sem acoplamento direto

### 4. Dependency Injection

Todos os serviços usam DI:
```csharp
services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
```

**Benefício:** Desacoplamento, testabilidade, flexibilidade

### 5. Repository Pattern

`IOrderRepository` abstrai acesso a dados:
- Implementação atual: In-Memory
- Futuro: Banco de dados SQL
- Sem mudança no código que usa o repositório

## 📊 Fluxo de Dados Completo

### Request/Response (Order API)

```
CLIENT
  │
  ├─ POST /api/orders
  │   {customerId, items[]}
  │
  ▼
OrdersController.CreateOrder()
  │
  ├─ Validação
  │
  ▼
OrderService.CreateOrderAsync()
  │
  ├─ Cálculo de total
  │
  ├─ OrderRepository.CreateAsync()
  │   └─ Gera ID, Timestamp
  │
  ├─ EventPublisher.PublishAsync()
  │   └─ Serializa para JSON
  │   └─ Publica em RabbitMQ
  │
  ▼
HTTP 201 Created
  {id, customerId, items[], total, status, createdAt}
```

### Event Flow (Async via RabbitMQ)

```
ORDER.CREATED EVENT
{
  "eventId": "uuid",
  "orderId": "uuid",
  "customerId": "uuid",
  "items": [{productId, quantity, price}],
  "total": 99.98,
  "createdAt": "2026-04-27T10:30:00Z"
}
  │
  ├─► Channel: payment-queue
  │      PaymentService consumes
  │      └─ Processa pagamento
  │      └─ Publica payment.processed
  │
  ├─► Channel: notification-queue-order
  │      NotificationService consumes
  │      └─ Envia email de confirmação
  │
  └─► Exchange: order-exchange
         Topic: order.created
         (Roteado para todas as filas acima)


PAYMENT.PROCESSED EVENT
{
  "eventId": "uuid",
  "orderId": "uuid",
  "status": "approved|rejected",
  "processedAt": "2026-04-27T10:31:00Z"
}
  │
  ├─► Channel: inventory-queue
  │      InventoryService consumes (se approved)
  │      └─ Reserva estoque
  │      └─ Publica inventory.reserved
  │
  ├─► Channel: notification-queue-payment
  │      NotificationService consumes
  │      └─ Envia email de pagamento
  │
  └─► Exchange: order-exchange
         Topic: payment.processed


INVENTORY.RESERVED EVENT
{
  "eventId": "uuid",
  "orderId": "uuid",
  "status": "reserved|insufficient",
  "reservedAt": "2026-04-27T10:32:00Z"
}
  │
  └─► Channel: notification-queue-inventory
         NotificationService consumes
         └─ Envia email de confirmação final
```

## 🔐 Confiabilidade e Resiliência

### Persistência de Mensagens

```csharp
var properties = new BasicProperties
{
    Persistent = true,
    ContentType = "application/json",
    DeliveryMode = DeliveryModes.Persistent
};
```

- Mensagens são persisted em disco
- Sobrevivem a reinicializações do RabbitMQ

### Manual Acknowledgment (ACK)

```csharp
await _channel.BasicAckAsync(ea.DeliveryTag, false);
```

- Mensagem só é removida da fila após processamento bem-sucedido
- Em caso de erro: `BasicNackAsync()` com `requeue=true`

### Recuperação Automática

```csharp
AutomaticRecoveryEnabled = true,
NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
```

- Reconecta automaticamente se a conexão cair
- Reestablish de channels e consumers

### QoS (Quality of Service)

```csharp
await _channel.BasicQosAsync(0, 1, false);
```

- Só envia 1 mensagem por vez para cada consumer
- Garante processamento sequencial e previsível

## 📈 Escalabilidade

### Horizontalização

Você pode escalar cada serviço independentemente:

```bash
# Versão 1: Um Payment Service
# Versão 2: Múltiplos Payment Services em paralelo
# Todos competindo pela mesma fila (load balancing automático)

# RabbitMQ distribui automaticamente entre consumers
```

### Load Balancing Automático

```
payment-queue
  ├─ Message 1 → Payment Service #1
  ├─ Message 2 → Payment Service #2
  ├─ Message 3 → Payment Service #1 (terminou primeiro)
  └─ Message 4 → Payment Service #2
```

## 🧪 Testabilidade

### Mocks Injetáveis

```csharp
// Produção
services.AddScoped<IPaymentProcessor, MockPaymentProcessor>();

// Teste
services.AddScoped<IPaymentProcessor, TestPaymentProcessor>();
```

### Eventos Simuláveis

```csharp
// Publicar evento de teste direto
var testEvent = new OrderCreatedEvent(
    orderId: Guid.NewGuid(),
    customerId: Guid.NewGuid(),
    items: new List<OrderItem> { ... },
    total: 100m
);
await publisher.PublishAsync(testEvent, "order.created");
```

## 🔍 Monitoramento e Logging

Cada serviço registra eventos chave:

```
✅ Evento publicado
📦 Evento recebido
💳 Processando pagamento
💰 Pagamento APPROVED/REJECTED
✅ Estoque reservado
📧 Notificação enviada
❌ Erro ao processar
🛑 Serviço encerrado
```

## 📋 Decisões de Arquitetura

| Decisão | Motivo | Alternativa Rejeitada |
|---------|--------|----------------------|
| RabbitMQ Topic Exchange | Roteamento flexível com wildcards | Direct Exchange (menos flexível) |
| Manual ACK | Garantir entrega confiável | Auto ACK (pode perder mensagens) |
| Worker Services | Processamento em background contínuo | Scheduled Tasks (menos responsivo) |
| In-Memory Repository | Simplicidade e demo | SQL Database (mais complexo) |
| JSON para eventos | Padrão de indústria | XML (verboso) |
| Shared Library | Reutilização de código | Duplicação (violaria DRY) |

## 🚀 Próximos Passos de Evolução

1. **Banco de Dados Real**
   - Substituir InMemoryOrderRepository por EF Core + SQL
   - Adicionar persistência durável

2. **Compensating Transactions (SAGA completo)**
   - Rollback de pagamentos se estoque falhar
   - Reembolsos automáticos

3. **Event Store**
   - Salvar cada evento em banco de dados
   - Auditoria completa
   - Event sourcing puro

4. **Caching Distribuído**
   - Redis para cache de pedidos
   - Invalidação inteligente

5. **API Gateway**
   - Ocelot ou similar
   - Rate limiting
   - Autenticação centralizada

6. **Testes Automatizados**
   - Unit tests para services
   - Integration tests com testcontainers
   - Load tests

7. **Kubernetes Deployment**
   - Containerização com Docker
   - Orquestração com K8s
   - Service discovery

---

**Documentação atualizada:** 27 de abril de 2026

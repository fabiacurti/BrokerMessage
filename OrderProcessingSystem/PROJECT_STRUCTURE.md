# 🎨 Estrutura Visual do Projeto

## 📊 Visão Geral da Solução

```
╔════════════════════════════════════════════════════════════════════════════╗
║                   ORDER PROCESSING SYSTEM - .NET 8.0                       ║
║                          COM RABBITMQ MESSAGING                            ║
╚════════════════════════════════════════════════════════════════════════════╝

                              ┌─────────────┐
                              │   CLIENTE   │
                              │ (HTTP/REST) │
                              └──────┬──────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │ POST /api/orders                │
                    │ GET /api/orders/{id}            │
                    │ GET /api/orders                 │
                    └────────────────┬────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │    ORDER API                    │
                    │  (ASP.NET Core)                 │
                    │  Porta: 5000                    │
                    │                                 │
                    │  • Controller                   │
                    │  • Service Layer                │
                    │  • Repository Pattern           │
                    │  • Publish Events               │
                    └────────────────┬────────────────┘
                                     │
                   ┌─────────────────▼──────────────────┐
                   │  RabbitMQ Message Bus             │
                   │  (Exchange: order-exchange)       │
                   │  (Type: Topic)                    │
                   │                                  │
                   │  Routing Keys:                    │
                   │  • order.created                  │
                   │  • payment.processed              │
                   │  • inventory.reserved             │
                   └─────────────────┬──────────────────┘
                                     │
        ┌────────────────────────────┼────────────────────────────┐
        │                            │                            │
    ┌───▼─────────┐         ┌────────▼────────┐         ┌────────▼────────┐
    │ PAYMENT     │         │  INVENTORY      │         │  NOTIFICATION   │
    │ SERVICE     │         │  SERVICE        │         │  SERVICE        │
    │             │         │                 │         │                 │
    │ Worker      │         │ Worker Service  │         │ Worker Service  │
    │             │         │                 │         │                 │
    │ Consumes:   │         │ Consumes:       │         │ Consumes:       │
    │ • order.    │         │ • payment.      │         │ • order.        │
    │   created   │         │   processed     │         │   created       │
    │             │         │                 │         │ • payment.      │
    │ Processes:  │         │ Processes:      │         │   processed     │
    │ • Payment   │         │ • Stock Check   │         │ • inventory.    │
    │   (80%)     │         │ • Reservation   │         │   reserved      │
    │             │         │                 │         │                 │
    │ Publishes:  │         │ Publishes:      │         │ Publishes:      │
    │ • payment.  │         │ • inventory.    │         │ • Email/SMS     │
    │   processed │         │   reserved      │         │   notifications │
    └─────────────┘         └─────────────────┘         └─────────────────┘
```

## 🗂️ Estrutura de Diretórios Detalhada

```
OrderProcessingSystem/
│
├── 📋 Documentação
│   ├── README.md                 (Principal - Início aqui!)
│   ├── QUICKSTART.md             (5 minutos de setup)
│   ├── TESTING.md                (Exemplos de teste)
│   ├── ARCHITECTURE.md           (Detalhes técnicos)
│   ├── DEVELOPMENT.md            (Guia para devs)
│   ├── INDEX.md                  (Índice e navegação)
│   └── PROJECT_STRUCTURE.md      (Este arquivo)
│
├── 🐳 Infraestrutura
│   ├── docker-compose.yml        (RabbitMQ setup)
│   ├── .env.example              (Variáveis de ambiente)
│   └── .gitignore                (Git configuration)
│
├── 🚀 Scripts
│   ├── start.sh                  (Iniciar sistema)
│   └── stop.sh                   (Parar sistema)
│
├── 📦 Solução Visual Studio
│   └── OrderProcessingSystem.sln
│
└── 📁 Código Fonte (src/)
    │
    ├─ Shared/                    (Biblioteca Compartilhada)
    │  ├── Shared.csproj
    │  ├── Events.cs              ← Modelos de Eventos
    │  │   ├── IntegrationEvent (base)
    │  │   ├── OrderCreatedEvent
    │  │   ├── PaymentProcessedEvent
    │  │   └── InventoryReservedEvent
    │  │
    │  └── RabbitMqEventBus.cs    ← Implementação RabbitMQ
    │      ├── IEventPublisher
    │      │   └── RabbitMqEventPublisher
    │      │
    │      └── IEventConsumer
    │          └── RabbitMqEventConsumer
    │
    ├─ OrderApi/                  (ASP.NET Core Web API)
    │  ├── OrderApi.csproj
    │  │
    │  ├── Program.cs             ← Configuração & DI
    │  ├── appsettings.json
    │  │
    │  ├── Models.cs              ← DTOs
    │  │   ├── Order
    │  │   ├── OrderItemDto
    │  │   ├── CreateOrderRequest
    │  │   └── OrderResponse
    │  │
    │  ├── Controllers/
    │  │  └── OrdersController.cs ← REST Endpoints
    │  │      ├── POST /api/orders
    │  │      ├── GET /api/orders/{id}
    │  │      ├── GET /api/orders
    │  │      └── GET /api/orders/health
    │  │
    │  ├── Services/
    │  │  └── OrderService.cs     ← Lógica de Negócio
    │  │      ├── CreateOrderAsync
    │  │      ├── GetOrderAsync
    │  │      ├── GetAllOrdersAsync
    │  │      └── UpdateOrderStatusAsync
    │  │
    │  ├── Repositories/
    │  │  └── OrderRepository.cs  ← Data Access Layer
    │  │      ├── InMemoryOrderRepository
    │  │      ├── CreateAsync
    │  │      ├── GetByIdAsync
    │  │      ├── UpdateAsync
    │  │      └── GetAllAsync
    │  │
    │  └── Properties/
    │     └── launchSettings.json
    │
    ├─ PaymentService/            (Worker Service - Pagamentos)
    │  ├── PaymentService.csproj
    │  ├── Program.cs             ← Setup & DI
    │  ├── appsettings.json
    │  │
    │  ├── IPaymentProcessor.cs   ← Processador de Pagamentos
    │  │  └── MockPaymentProcessor
    │  │      ├── ProcessPaymentAsync (80% aprovação)
    │  │      └── Publica payment.processed
    │  │
    │  └── PaymentProcessingWorker.cs ← Background Service
    │     ├── Consome: order.created
    │     ├── Processa pagamento
    │     └── Publica: payment.processed
    │
    ├─ InventoryService/          (Worker Service - Estoque)
    │  ├── InventoryService.csproj
    │  ├── Program.cs
    │  ├── appsettings.json
    │  │
    │  ├── IInventoryService.cs   ← Gerenciador de Estoque
    │  │  └── MockInventoryService
    │  │      └── ReserveInventoryAsync
    │  │
    │  └── InventoryProcessingWorker.cs ← Background Service
    │     ├── Consome: payment.processed
    │     ├── Valida estoque
    │     └── Publica: inventory.reserved
    │
    └─ NotificationService/       (Worker Service - Notificações)
       ├── NotificationService.csproj
       ├── Program.cs
       ├── appsettings.json
       │
       ├── INotificationSender.cs ← Sender de Notificações
       │  └── MockNotificationSender
       │      ├── SendOrderCreatedNotificationAsync
       │      ├── SendPaymentNotificationAsync
       │      └── SendInventoryNotificationAsync
       │
       └── NotificationWorker.cs  ← Background Service
          ├── Consome: order.created
          ├── Consome: payment.processed
          ├── Consome: inventory.reserved
          └── Emite notificações (email/SMS simulado)
```

## 🔄 Fluxo de Eventos

```
┌───────────────────────────────────────────────────────────────┐
│                    FLUXO COMPLETO DE PEDIDO                   │
└───────────────────────────────────────────────────────────────┘

1️⃣  CLIENTE
    │
    ├─► POST /api/orders
    │   {customerId, items[]}
    │
    └─► HTTP 201 Created
        {id, customerId, items[], total, status, createdAt}


2️⃣  ORDER API
    │
    ├─► OrderService.CreateOrderAsync()
    │   ├─ Validar dados
    │   ├─ Calcular total
    │   ├─ Persistir (Repository)
    │   └─ Publicar evento
    │
    └─► PublishAsync(OrderCreatedEvent, "order.created")


3️⃣  RABBITMQ
    │
    ├─ Exchange: order-exchange (type: topic)
    ├─ Routing Key: order.created
    │
    └─► Distribuir para filas:
        ├─ payment-queue
        ├─ notification-queue-order
        └─ (outros consumidores)


4️⃣  PAYMENT SERVICE (Paralelo 1)
    │
    ├─► Consome de payment-queue
    ├─► PaymentProcessingWorker.ExecuteAsync()
    │   ├─ Recebe OrderCreatedEvent
    │   ├─ MockPaymentProcessor.ProcessPaymentAsync()
    │   │  ├─ Simula delay
    │   │  ├─ 80% aprovação / 20% rejeição
    │   │  └─ Gera PaymentProcessedEvent
    │   └─ PublishAsync(PaymentProcessedEvent, "payment.processed")
    │
    └─► ACK mensagem original


5️⃣  INVENTORY SERVICE (Paralelo 2)
    │
    ├─► Consome de inventory-queue (payment.processed)
    ├─► InventoryProcessingWorker.ExecuteAsync()
    │   ├─ Recebe PaymentProcessedEvent
    │   ├─ Verifica status = "approved"
    │   ├─ MockInventoryService.ReserveInventoryAsync()
    │   │  ├─ Valida disponibilidade
    │   │  ├─ Decrementa estoque
    │   │  └─ Retorna reserved/insufficient
    │   └─ PublishAsync(InventoryReservedEvent, "inventory.reserved")
    │
    └─► ACK mensagem original


6️⃣  NOTIFICATION SERVICE (Paralelo 3)
    │
    ├─► Consome de múltiplas filas
    │   ├─ notification-queue-order (order.created)
    │   ├─ notification-queue-payment (payment.processed)
    │   └─ notification-queue-inventory (inventory.reserved)
    │
    ├─► NotificationWorker.ExecuteAsync()
    │   ├─ Task 1: MockNotificationSender.SendOrderCreatedNotificationAsync()
    │   │           📧 Email: "Obrigado por sua compra!"
    │   │
    │   ├─ Task 2: MockNotificationSender.SendPaymentNotificationAsync()
    │   │           📧 Email: "Pagamento aprovado/rejeitado"
    │   │
    │   └─ Task 3: MockNotificationSender.SendInventoryNotificationAsync()
    │               📧 Email: "Pedido confirmado/estoque insuficiente"
    │
    └─► ACK todas as mensagens


7️⃣  CLIENTE (Poll)
    │
    ├─► GET /api/orders/{id}
    │
    └─► Pedido com status atualizado
```

## 📊 Tabela de Componentes

| Componente | Tipo | Linguagem | Porta | Responsabilidade |
|-----------|------|-----------|-------|-----------------|
| Order API | Web API | C#/.NET | 5000 | Receber pedidos, publicar eventos |
| Payment Service | Worker | C#/.NET | N/A | Processar pagamentos |
| Inventory Service | Worker | C#/.NET | N/A | Reservar estoque |
| Notification Service | Worker | C#/.NET | N/A | Enviar notificações |
| RabbitMQ | Message Broker | Erlang | 5672 | Roteamento de mensagens |
| RabbitMQ Management | Web UI | JavaScript | 15672 | Monitoramento |

## 🔐 Padrões e Tecnologias

```
┌─────────────────────────────────────────────┐
│         PADRÕES DE DESIGN UTILIZADOS        │
├─────────────────────────────────────────────┤
│ • Pub/Sub (RabbitMQ)                        │
│ • Event Sourcing (Simplificado)             │
│ • SAGA Pattern (Simplificado)               │
│ • Repository Pattern                        │
│ • Service Layer Pattern                     │
│ • Dependency Injection                      │
│ • Factory Pattern (Implícito)               │
│ • Background Service Pattern                │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│       TECNOLOGIAS E FRAMEWORKS              │
├─────────────────────────────────────────────┤
│ .NET 8.0                                    │
│ ASP.NET Core (Web API)                      │
│ RabbitMQ Client                             │
│ Microsoft.Extensions.Hosting                │
│ Entity Framework Core (Futuro)              │
│ Serilog (Futuro)                            │
│ xUnit (Futuro)                              │
└─────────────────────────────────────────────┘
```

## 📈 Escalabilidade

```
ANTES (Monolítico):
┌─────────────────────────────┐
│  Aplicação Monolítica        │
│  ├─ Orders                  │
│  ├─ Payments                │
│  ├─ Inventory               │
│  └─ Notifications           │
└─────────────────────────────┘
❌ Acoplado, difícil escalar

DEPOIS (Microserviços com Eventos):
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
│ Order    │  │ Payment  │  │Inventory │  │Notification │
│  API     │  │ Service  │  │ Service  │  │   Service   │
└────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬──────┘
     │             │              │               │
     └─────────────┴──────────────┴───────────────┘
                   │
            ┌──────▼───────┐
            │   RabbitMQ   │
            │  (Messaging) │
            └──────────────┘

✅ Desacoplado, escalável horizontalmente
✅ Cada serviço pode ser replicado
✅ Load balancing automático
```

## 🚀 Próximos Passos de Evolução

```
FASE 1: ✅ CONCLUÍDO (Versão Atual)
├─ Arquitetura baseada em eventos
├─ RabbitMQ com Topic Exchange
├─ 4 serviços desacoplados
└─ Simulações de negócio

FASE 2: Banco de Dados Real
├─ [ ] EF Core + SQL Server
├─ [ ] Migrations
├─ [ ] Índices e Performance
└─ [ ] Backup estratégia

FASE 3: Resiliência e Monitoramento
├─ [ ] Serilog + Application Insights
├─ [ ] ELK Stack (opcional)
├─ [ ] Health Checks
└─ [ ] Circuit Breaker

FASE 4: Testes Automatizados
├─ [ ] Unit Tests (xUnit)
├─ [ ] Integration Tests
├─ [ ] Load Tests
└─ [ ] E2E Tests

FASE 5: Containerização e Orquestração
├─ [ ] Dockerfiles
├─ [ ] Kubernetes Manifests
├─ [ ] Helm Charts
└─ [ ] CI/CD Pipelines

FASE 6: Segurança e Produção
├─ [ ] SSL/TLS
├─ [ ] Autenticação (OAuth/JWT)
├─ [ ] Rate Limiting
└─ [ ] API Gateway
```

---

**Diagrama atualizado:** 27 de abril de 2026

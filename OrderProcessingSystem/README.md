# 🎯 Order Processing System - RabbitMQ com .NET
Um sistema completo e profissional de processamento de pedidos baseado em eventos, utilizando .NET 8.0 e RabbitMQ com padrão de arquitetura desacoplada.

## 📋 Visão Geral

Este projeto demonstra uma arquitetura orientada a eventos com múltiplos serviços independentes comunicando-se através de mensagens RabbitMQ. O fluxo completo de um pedido passa por validação, pagamento, reserva de estoque e notificação.

```
┌─────────────┐
│  Order API  │ (ASP.NET Core Web API)
│ (Publica)   │
└──────┬──────┘
       │ order.created
       ▼
┌──────────────────┐
│ Exchange: topic  │
│ order-exchange   │
└────┬─────┬──────┘
     │     │
     │     └─► Routing Key: payment.processed ──► inventory-queue
     │        
     └─► Routing Key: order.created ──► payment-queue
         
┌─────────────────────────────────────────────────────────────┐
│                   Worker Services                           │
├─────────────┬──────────────────┬──────────────────────────┤
│  Payment    │   Inventory      │    Notification          │
│  Service    │   Service        │    Service               │
│             │                  │                          │
│ Consome:    │ Consome:         │ Consome:                 │
│ order.      │ payment.         │ order.created            │
│ created     │ processed        │ payment.processed        │
│             │                  │ inventory.reserved       │
│ Publica:    │ Publica:         │                          │
│ payment.    │ inventory.       │ Envia Notificações:      │
│ processed   │ reserved         │ • Email/SMS              │
└─────────────┴──────────────────┴──────────────────────────┘
```

## 🏗️ Arquitetura

### Projetos

1. **Shared** - Biblioteca compartilhada
   - Modelos de eventos
   - Interfaces e implementações de RabbitMQ
   - Classes base para eventos de integração

2. **OrderApi** - ASP.NET Core Web API
   - Endpoints REST para gerenciar pedidos
   - Publicador de eventos `order.created`
   - Swagger/OpenAPI documentado

3. **PaymentService** - Worker Service
   - Consome eventos `order.created`
   - Processa pagamentos (simulado com 80% aprovação)
   - Publica eventos `payment.processed`

4. **InventoryService** - Worker Service
   - Consome eventos `payment.processed`
   - Reserva estoque (validação simples)
   - Publica eventos `inventory.reserved`

5. **NotificationService** - Worker Service
   - Consome todos os eventos
   - Simula envio de notificações por email/SMS
   - Fornece feedback ao usuário

## 🐰 RabbitMQ Design

### Exchange
- **Nome:** `order-exchange`
- **Tipo:** Topic
- **Durável:** Sim

### Filas
```
payment-queue
  ├─ Binding: order.created
  └─ Consumer: PaymentService

inventory-queue
  ├─ Binding: payment.processed
  └─ Consumer: InventoryService

notification-queue-order
  ├─ Binding: order.created
  └─ Consumer: NotificationService

notification-queue-payment
  ├─ Binding: payment.processed
  └─ Consumer: NotificationService

notification-queue-inventory
  ├─ Binding: inventory.reserved
  └─ Consumer: NotificationService
```

### Routing Keys
- `order.created` - Publicado pela Order API
- `payment.processed` - Publicado pelo Payment Service
- `inventory.reserved` - Publicado pelo Inventory Service

## 🚀 Pré-requisitos

- .NET SDK 8.0 ou superior
- Docker e Docker Compose (para RabbitMQ)
- curl ou Postman (para testar endpoints)

## 📦 Instalação e Setup

### 1. Clone o repositório

```bash
cd /home/fabiacurti/projetos/BrokerMessage/OrderProcessingSystem
```

### 2. Inicie o RabbitMQ com Docker

```bash
docker-compose up -d
```

Aguarde o RabbitMQ estar pronto (verifique com `docker-compose logs`):

```bash
docker-compose logs rabbitmq | grep "Server startup complete"
```

### 3. Restaure as dependências

```bash
dotnet restore
```

### 4. Build da solução

```bash
dotnet build
```

## 🏃 Executando os Serviços

Abra **4 terminais diferentes** e execute cada serviço em um terminal:

### Terminal 1: Order API
```bash
cd src/OrderApi
dotnet run
```

Esperado:
```
✅ Order API iniciada!
info: Microsoft.AspNetCore.Hosting.Hosting
Listening on http://localhost:5000
```

### Terminal 2: Payment Service
```bash
cd src/PaymentService
dotnet run
```

Esperado:
```
✅ Payment Service iniciado!
🚀 Payment Service iniciado
```

### Terminal 3: Inventory Service
```bash
cd src/InventoryService
dotnet run
```

Esperado:
```
✅ Inventory Service iniciado!
🚀 Inventory Service iniciado
```

### Terminal 4: Notification Service
```bash
cd src/NotificationService
dotnet run
```

Esperado:
```
✅ Notification Service iniciado!
🚀 Notification Service iniciado
```

## 🧪 Testando o Sistema

### 1. Acessar Swagger da Order API

Abra no navegador:
```
http://localhost:5000/swagger
```

### 2. Criar um Pedido (via cURL)

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 2,
        "price": 50.00
      },
      {
        "productId": "00000000-0000-0000-0000-000000000002",
        "quantity": 1,
        "price": 100.00
      }
    ]
  }'
```

**Resposta esperada:**
```json
{
  "id": "a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "items": [...],
  "total": 200.00,
  "status": "pending",
  "createdAt": "2026-04-27T10:30:00.000Z"
}
```

### 3. Consultar Pedido

```bash
curl http://localhost:5000/api/orders/a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
```

### 4. Listar Todos os Pedidos

```bash
curl http://localhost:5000/api/orders
```

### 5. Health Check

```bash
curl http://localhost:5000/api/orders/health
```

## 📊 Fluxo de Mensagens Esperado

Ao criar um pedido, observe os logs em cada terminal:

**Terminal 1 (Order API):**
```
✅ Evento publicado: OrderCreatedEvent | Routing Key: order.created
```

**Terminal 2 (Payment Service):**
```
📦 Evento recebido: OrderCreatedEvent | OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
💳 Processando pagamento para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5 | Valor: 200.00
💰 Pagamento APPROVED para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
✅ Evento publicado: PaymentProcessedEvent | Routing Key: payment.processed
```

**Terminal 3 (Inventory Service):**
```
💳 Evento recebido: PaymentProcessedEvent | OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5 | Status: approved
📦 Processando reserva de estoque para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
✅ Estoque reservado | Produto: 00000000-0000-0000-0000-000000000001 | Qtd: 2
✅ Evento publicado: InventoryReservedEvent | Routing Key: inventory.reserved
```

**Terminal 4 (Notification Service):**
```
📧 [NOTIFICAÇÃO] Pedido criado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - [EMAIL ENVIADO] Obrigado por sua compra!

📧 [NOTIFICAÇÃO] Pagamento aprovado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - [EMAIL ENVIADO] Seu pagamento foi confirmado!

📧 [NOTIFICAÇÃO] Estoque reservado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - [EMAIL ENVIADO] Seu pedido foi confirmado e será processado!
```

## 🔍 Monitorar RabbitMQ

Acesse a interface de gerenciamento:

```
http://localhost:15672
```

**Credenciais padrão:**
- Usuário: `guest`
- Senha: `guest`

Aqui você pode:
- Visualizar exchanges e filas
- Monitorar mensagens
- Testar conexões
- Ver estatísticas

## ⚙️ Configurações

Todos os serviços leem `appsettings.json` para configurar RabbitMQ:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  }
}
```

Para alterar o host RabbitMQ em produção, edite esse arquivo.

## 🛑 Parar os Serviços

1. Feche cada terminal com `Ctrl+C`
2. Parar RabbitMQ: `docker-compose down`
3. (Opcional) Remover volume: `docker-compose down -v`

## 📝 Requisitos Técnicos Implementados

✅ **Mensageria**
- RabbitMQ.Client 6.8.1
- Publisher confirmável (mensagens persistentes)
- Manual ACK nos consumers
- Retry automático em caso de erro

✅ **Confiabilidade**
- Mensagens persistentes (DeliveryMode.Persistent)
- Tratamento de exceções em todos os consumers
- Logs detalhados em cada operação
- Graceful shutdown

✅ **Arquitetura**
- Dependency Injection completo
- Clean Architecture (separação por responsabilidades)
- Interfaces bem definidas
- Padrão de eventos de integração

## 🎓 Conceitos Demonstrados

1. **Arquitetura Orientada a Eventos (Event-Driven)**
2. **Padrão de Publicador/Subscritor**
3. **Desacoplamento de Serviços**
4. **RabbitMQ Topic Exchange com Routing Keys**
5. **Processamento Assíncrono de Mensagens**
6. **Resiliência e Recuperação Automática**
7. **Logging Distribuído**
8. **Simulação de Processos de Negócio**

## 🚨 Troubleshooting

### Erro: "Connection refused"
```
Solução: Verifique se RabbitMQ está rodando
docker-compose ps
docker-compose logs rabbitmq
```

### Erro: "Queue already exists"
```
Solução: Limpar volumes do Docker
docker-compose down -v
docker-compose up -d
```

### Mensagens não sendo processadas
```
Solução: Verifique se todos os serviços estão rodando
- Order API deve estar rodando na porta 5000
- RabbitMQ Management em http://localhost:15672
- Verifique logs de cada serviço
```

## 📚 Referências

- [RabbitMQ.Client GitHub](https://github.com/rabbitmq/rabbitmq-dotnet-client)
- [Microsoft - Event-driven architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
- [ASP.NET Core Background Tasks](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

## 📄 Licença

Este projeto é fornecido como exemplo educacional.

## 👨‍💻 Estrutura de Pastas Final

```
OrderProcessingSystem/
├── OrderProcessingSystem.sln
├── docker-compose.yml
├── README.md
└── src/
    ├── Shared/
    │   ├── Shared.csproj
    │   ├── Events.cs
    │   └── RabbitMqEventBus.cs
    ├── OrderApi/
    │   ├── OrderApi.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── Models.cs
    │   ├── Controllers/
    │   │   └── OrdersController.cs
    │   ├── Services/
    │   │   └── OrderService.cs
    │   ├── Repositories/
    │   │   └── OrderRepository.cs
    │   └── Properties/
    │       └── launchSettings.json
    ├── PaymentService/
    │   ├── PaymentService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── IPaymentProcessor.cs
    │   └── PaymentProcessingWorker.cs
    ├── InventoryService/
    │   ├── InventoryService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── IInventoryService.cs
    │   └── InventoryProcessingWorker.cs
    └── NotificationService/
        ├── NotificationService.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── INotificationSender.cs
        └── NotificationWorker.cs
```

---

**Desenvolvido com ❤️ usando .NET 8.0 e RabbitMQ**

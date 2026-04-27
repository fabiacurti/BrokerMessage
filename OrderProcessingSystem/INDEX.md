# 📑 Índice e Guia de Navegação

## 🎯 Começar Aqui

**Primeiro acesso?** Leia nesta ordem:

1. **[QUICKSTART.md](QUICKSTART.md)** (5 minutos)
   - Setup rápido
   - Primeiras operações
   - Links úteis

2. **[README.md](README.md)** (30 minutos)
   - Visão geral do sistema
   - Arquitetura
   - Instruções completas de instalação

3. **[TESTING.md](TESTING.md)** (20 minutos)
   - Exemplos de testes com curl
   - Cenários de teste
   - Troubleshooting

4. **[ARCHITECTURE.md](ARCHITECTURE.md)** (Aprofundado)
   - Componentes detalhados
   - Padrões de design
   - Decisões técnicas

## 📁 Estrutura do Projeto

```
OrderProcessingSystem/
│
├── 📄 README.md                    ← Documentação principal
├── 📄 QUICKSTART.md                ← Guia rápido
├── 📄 TESTING.md                   ← Exemplos de teste
├── 📄 ARCHITECTURE.md              ← Detalhes técnicos
├── 📄 INDEX.md                     ← Este arquivo
│
├── 📜 OrderProcessingSystem.sln    ← Solução Visual Studio
├── 🐳 docker-compose.yml            ← RabbitMQ setup
│
├── 🚀 start.sh                     ← Script de inicialização
├── 🛑 stop.sh                      ← Script para parar
│
├── 📋 .gitignore                   ← Configuração Git
├── 📋 .env.example                 ← Exemplo de variáveis
│
└── 📁 src/                         ← Código fonte
    │
    ├── 📁 Shared/
    │   ├── Shared.csproj
    │   ├── Events.cs               ← Modelos de eventos
    │   └── RabbitMqEventBus.cs     ← Implementação RabbitMQ
    │
    ├── 📁 OrderApi/                ← ASP.NET Core Web API (Porta 5000)
    │   ├── OrderApi.csproj
    │   ├── Program.cs              ← Configuração e DI
    │   ├── appsettings.json
    │   ├── Models.cs               ← DTOs e modelos
    │   ├── Controllers/
    │   │   └── OrdersController.cs ← REST endpoints
    │   ├── Services/
    │   │   └── OrderService.cs     ← Lógica de negócio
    │   ├── Repositories/
    │   │   └── OrderRepository.cs  ← Acesso a dados
    │   └── Properties/
    │       └── launchSettings.json
    │
    ├── 📁 PaymentService/          ← Worker Service (Pagamentos)
    │   ├── PaymentService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── IPaymentProcessor.cs    ← Interface e implementação
    │   └── PaymentProcessingWorker.cs ← Background service
    │
    ├── 📁 InventoryService/        ← Worker Service (Estoque)
    │   ├── InventoryService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── IInventoryService.cs
    │   └── InventoryProcessingWorker.cs
    │
    └── 📁 NotificationService/     ← Worker Service (Notificações)
        ├── NotificationService.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── INotificationSender.cs
        └── NotificationWorker.cs
```

## 🎓 Aprender

### Conceitos Implementados

- **Arquitetura Orientada a Eventos (EDA)**
  - Ver: [ARCHITECTURE.md#padrões-de-design](ARCHITECTURE.md)

- **RabbitMQ com Topic Exchange**
  - Ver: [README.md#rabbitmq-design](README.md#rabbitmq-design)
  - Código: `src/Shared/RabbitMqEventBus.cs`

- **Padrão Pub/Sub**
  - Ver: [ARCHITECTURE.md#publisher-subscriber](ARCHITECTURE.md)

- **SAGA Pattern (Simplificado)**
  - Ver: [ARCHITECTURE.md#saga-pattern](ARCHITECTURE.md)

- **Dependency Injection**
  - Código: `src/OrderApi/Program.cs`
  - Código: `src/PaymentService/Program.cs`

- **Repository Pattern**
  - Código: `src/OrderApi/Repositories/OrderRepository.cs`

- **Background Services (.NET)**
  - Código: `src/PaymentService/PaymentProcessingWorker.cs`

### Arquivos-Chave por Tópico

#### Publicação de Eventos
- `src/OrderApi/Services/OrderService.cs` (linha 40-50)

#### Consumo de Eventos
- `src/PaymentService/PaymentProcessingWorker.cs` (linha 30-60)
- `src/Shared/RabbitMqEventBus.cs` (classe `RabbitMqEventConsumer`)

#### Modelos e DTOs
- `src/Shared/Events.cs` (eventos base)
- `src/OrderApi/Models.cs` (DTOs da API)

#### Endpoints REST
- `src/OrderApi/Controllers/OrdersController.cs`

## 🧪 Teste

### Guia Rápido de Teste

```bash
# 1. Setup
./start.sh

# 2. Em outro terminal - Criar pedido
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [{"productId": "00000000-0000-0000-0000-000000000001", "quantity": 2, "price": 50}]
  }'

# 3. Observe os logs em cada terminal
```

Ver [TESTING.md](TESTING.md) para exemplos completos.

### Cenários de Teste

| Cenário | Arquivo | Descrição |
|---------|---------|-----------|
| Criar pedido | [TESTING.md#passo-2](TESTING.md) | Fluxo básico |
| Múltiplos itens | [TESTING.md#cenário-1](TESTING.md) | Cálculo de total |
| Diferentes clientes | [TESTING.md#cenário-3](TESTING.md) | Isolamento |
| Testes de erro | [TESTING.md#testes-de-erro](TESTING.md) | Validação |
| Teste de carga | [TESTING.md#teste-de-carga](TESTING.md) | Performance |

## 🔧 Configuração

### Variáveis de Ambiente

Ver [.env.example](.env.example)

Configurações por serviço:
- `src/OrderApi/appsettings.json`
- `src/PaymentService/appsettings.json`
- `src/InventoryService/appsettings.json`
- `src/NotificationService/appsettings.json`

### Docker

- **Build:** `docker-compose build`
- **Iniciar:** `docker-compose up -d`
- **Parar:** `docker-compose down`
- **Logs:** `docker-compose logs rabbitmq`
- **Limpar:** `docker-compose down -v`

## 📊 Fluxo de Dados

```
CLIENT (HTTP)
    ↓
ORDER API (/api/orders POST)
    ↓ publica
RABBITMQ (order.created)
    ↓ consome
PAYMENT SERVICE → publica payment.processed
INVENTORY SERVICE → publica inventory.reserved
NOTIFICATION SERVICE → emite logs de notificação
```

Ver [ARCHITECTURE.md#fluxo-de-dados-completo](ARCHITECTURE.md) para detalhes.

## 🎨 Design Patterns

| Padrão | Implementado em | Arquivo |
|--------|-----------------|---------|
| Repository | OrderApi | `Repositories/OrderRepository.cs` |
| Service Layer | OrderApi | `Services/OrderService.cs` |
| Dependency Injection | Todos | `Program.cs` em cada serviço |
| Publisher-Subscriber | Shared | `RabbitMqEventBus.cs` |
| Factory | Shared | Implícito em criação de eventos |
| Background Service | Workers | `PaymentProcessingWorker.cs` |

## 🐛 Troubleshooting

### Problema: "Connection refused"
**Solução:** [README.md#troubleshooting](README.md#troubleshooting)

### Problema: Porta em uso
**Solução:** [README.md#troubleshooting](README.md#troubleshooting)

### Problema: Mensagens não processam
**Solução:** [TESTING.md#debug-e-troubleshooting](TESTING.md#debug-e-troubleshooting)

## 📈 Próximos Passos

1. **Banco de Dados Real**
   - Substituir `InMemoryOrderRepository`
   - Implementar com EF Core + SQL Server

2. **Testes Automatizados**
   - Unit tests com xUnit
   - Integration tests com testcontainers

3. **Kubernetes**
   - Criar Dockerfiles
   - Deploy em K8s

4. **Observabilidade**
   - Serilog
   - Application Insights
   - ELK Stack

Ver [ARCHITECTURE.md#próximos-passos-de-evolução](ARCHITECTURE.md) para detalhes.

## 🔗 Links Externos

- **RabbitMQ Docs:** https://www.rabbitmq.com/documentation.html
- **.NET Worker Services:** https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- **ASP.NET Core:** https://learn.microsoft.com/en-us/aspnet/core/
- **Event-Driven Architecture:** https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven

## 📞 Suporte

Para problemas ou dúvidas:

1. Consulte a seção de [Troubleshooting](README.md#troubleshooting)
2. Verifique os [Testes](TESTING.md)
3. Revise a [Arquitetura](ARCHITECTURE.md)

## 📝 Licença

Projeto educacional de exemplo.

---

**Última atualização:** 27 de abril de 2026

**Versão:** 1.0.0

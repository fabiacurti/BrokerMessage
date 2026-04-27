# ⚡ Quick Start Guide

## 🚀 Em 5 Minutos

### 1. Inicie RabbitMQ (1 minuto)

```bash
cd /home/fabiacurti/projetos/BrokerMessage/OrderProcessingSystem
docker-compose up -d
```

**Aguarde:**
```bash
docker-compose logs rabbitmq | grep "Server startup complete"
```

### 2. Abra 4 Terminais

#### Terminal 1 - Order API
```bash
cd src/OrderApi
dotnet run
```

#### Terminal 2 - Payment Service
```bash
cd src/PaymentService
dotnet run
```

#### Terminal 3 - Inventory Service
```bash
cd src/InventoryService
dotnet run
```

#### Terminal 4 - Notification Service
```bash
cd src/NotificationService
dotnet run
```

### 3. Crie um Pedido (Terminal separado)

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
      }
    ]
  }'
```

### 4. Observe os Logs

Você verá mensagens em cada terminal mostrando o fluxo completo:
1. Order API: Pedido criado
2. Payment Service: Pagamento processado
3. Inventory Service: Estoque reservado
4. Notification Service: Notificações enviadas

**Pronto! 🎉**

## 📚 Links Úteis

- **API Swagger:** http://localhost:5000/swagger
- **RabbitMQ Dashboard:** http://localhost:15672 (guest/guest)
- **Documentação Completa:** [README.md](README.md)
- **Testes Detalhados:** [TESTING.md](TESTING.md)
- **Arquitetura:** [ARCHITECTURE.md](ARCHITECTURE.md)

## 🐳 Parar Tudo

```bash
# Feche cada terminal com Ctrl+C
# Depois

docker-compose down
```

## ⚠️ Problemas Comuns

| Problema | Solução |
|----------|---------|
| "Connection refused" | Verifique: `docker-compose ps` |
| Porta 5000 em uso | `lsof -i :5000` e mate o processo |
| Mensagens não processam | Verifique se todos 4 serviços estão rodando |
| RabbitMQ lento | `docker-compose restart` |

## 📝 Estrutura de Projetos

```
src/
├── Shared/           (Eventos e RabbitMQ)
├── OrderApi/         (Web API - porta 5000)
├── PaymentService/   (Worker - processa pagamentos)
├── InventoryService/ (Worker - reserva estoque)
└── NotificationService/ (Worker - envia notificações)
```

---

**Leia [README.md](README.md) para documentação completa** 📚

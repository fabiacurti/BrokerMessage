# 🧪 Guia Completo de Teste - Order Processing System

## 📋 Pré-requisitos para Testes

- curl ou Postman instalado
- Todos os 4 serviços rodando (Order API, Payment Service, Inventory Service, Notification Service)
- RabbitMQ rodando via docker-compose
- Terminais abertos para visualizar logs

## 🚀 Iniciando o Teste Completo

### Passo 1: Verificar se tudo está funcionando

#### Health Check da Order API

```bash
curl -i http://localhost:5000/api/orders/health
```

**Resposta esperada (HTTP 200):**
```json
{
  "status": "healthy",
  "service": "Order API"
}
```

### Passo 2: Criar Primeiro Pedido

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 2,
        "price": 49.99
      }
    ]
  }'
```

**Campos:**
- `customerId`: UUID do cliente
- `items`: Array de itens
  - `productId`: UUID do produto (use IDs pré-existentes)
  - `quantity`: Quantidade (número inteiro)
  - `price`: Preço unitário

**Resposta esperada (HTTP 201 Created):**
```json
{
  "id": "a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000001",
      "quantity": 2,
      "price": 49.99
    }
  ],
  "total": 99.98,
  "status": "pending",
  "createdAt": "2026-04-27T10:30:00.000Z"
}
```

**Copie o `id` do pedido para usar nos próximos passos!**

### Passo 3: Acompanhar Eventos nos Logs

Observe os terminais:

**Terminal 1 (Order API):**
```
✅ Evento publicado: OrderCreatedEvent | Routing Key: order.created
```

**Terminal 2 (Payment Service) - ~1-2 segundos depois:**
```
📦 Evento recebido: OrderCreatedEvent | OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
💳 Processando pagamento para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5 | Valor: 99.98
💰 Pagamento APPROVED para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
✅ Evento publicado: PaymentProcessedEvent | Routing Key: payment.processed
```

**Terminal 3 (Inventory Service) - ~1-2 segundos depois:**
```
💳 Evento recebido: PaymentProcessedEvent | OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5 | Status: approved
📦 Processando reserva de estoque para pedido a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
✅ Estoque reservado | Produto: 00000000-0000-0000-0000-000000000001 | Qtd: 2
✅ Evento publicado: InventoryReservedEvent | Routing Key: inventory.reserved
```

**Terminal 4 (Notification Service) - Paralelo com todos:**
```
📧 [NOTIFICAÇÃO] Pedido criado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - CustomerId: 550e8400-e29b-41d4-a716-446655440000
   - Total: R$99.98
   - [EMAIL ENVIADO] Obrigado por sua compra!

📧 [NOTIFICAÇÃO] Pagamento aprovado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - [EMAIL ENVIADO] Seu pagamento foi confirmado!

📧 [NOTIFICAÇÃO] Estoque reservado
   - OrderId: a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
   - [EMAIL ENVIADO] Seu pedido foi confirmado e será processado!
```

### Passo 4: Consultar Pedido Criado

Use o `id` do pedido retornado:

```bash
curl http://localhost:5000/api/orders/a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5
```

**Resposta esperada:**
```json
{
  "id": "a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "items": [...],
  "total": 99.98,
  "status": "pending",
  "createdAt": "2026-04-27T10:30:00.000Z"
}
```

### Passo 5: Listar Todos os Pedidos

```bash
curl http://localhost:5000/api/orders
```

**Resposta esperada:**
```json
[
  {
    "id": "a1b2c3d4-e5f6-4a5b-8c9d-e0f1a2b3c4d5",
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [...],
    "total": 99.98,
    "status": "pending",
    "createdAt": "2026-04-27T10:30:00.000Z"
  }
]
```

## 📝 Cenários de Teste Avançados

### Cenário 1: Múltiplos Itens no Pedido

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
        "price": 150.00
      },
      {
        "productId": "00000000-0000-0000-0000-000000000003",
        "quantity": 3,
        "price": 25.00
      }
    ]
  }'
```

**Total esperado:** (2×50) + (1×150) + (3×25) = R$ 275.00

### Cenário 2: Pedido com Valores Altos

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 5,
        "price": 999.99
      }
    ]
  }'
```

**Total esperado:** 5 × 999.99 = R$ 4.999,95

### Cenário 3: Diferentes Clientes

```bash
# Cliente A
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "11111111-1111-1111-1111-111111111111",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 1,
        "price": 100.00
      }
    ]
  }'

# Cliente B
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "22222222-2222-2222-2222-222222222222",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000002",
        "quantity": 2,
        "price": 50.00
      }
    ]
  }'
```

Depois liste todos: `curl http://localhost:5000/api/orders`

## ❌ Testes de Erro

### Erro 1: CustomerId Vazio

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000000",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 1,
        "price": 50.00
      }
    ]
  }'
```

**Resposta esperada (HTTP 400):**
```json
{
  "error": "CustomerId é obrigatório"
}
```

### Erro 2: Sem Items

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": []
  }'
```

**Resposta esperada (HTTP 400):**
```json
{
  "error": "Pedido deve ter pelo menos um item"
}
```

### Erro 3: Pedido Inexistente

```bash
curl http://localhost:5000/api/orders/00000000-0000-0000-0000-000000000000
```

**Resposta esperada (HTTP 404):**
```json
{
  "error": "Pedido não encontrado"
}
```

### Erro 4: Formato JSON Inválido

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{invalid json}'
```

**Resposta esperada (HTTP 400):** Erro de desserialização

## 🔄 Testes de Fila RabbitMQ

### Verificar Filas via Management UI

1. Abra http://localhost:15672
2. Login com guest/guest
3. Vá em "Queues"
4. Você deve ver:
   - `payment-queue`
   - `inventory-queue`
   - `notification-queue-order`
   - `notification-queue-payment`
   - `notification-queue-inventory`

### Verificar Exchange

1. Em "Exchanges"
2. Clique em `order-exchange`
3. Você deve ver os bindings:
   - `order.created` → `payment-queue`
   - `payment.processed` → `inventory-queue`
   - etc.

### Monitorar Mensagens em Tempo Real

1. Em "Queues"
2. Clique em uma fila (ex: `payment-queue`)
3. Role para baixo e clique "Get messages" para visualizar mensagens

## 📊 Exemplo de Teste Completo com Script

Salve como `test_complete_flow.sh`:

```bash
#!/bin/bash

echo "🧪 Iniciando teste completo..."

# Criar pedido
echo "📝 Criando pedido..."
RESPONSE=$(curl -s -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "quantity": 2,
        "price": 49.99
      }
    ]
  }')

echo "$RESPONSE" | jq '.'

# Extrair ID do pedido
ORDER_ID=$(echo "$RESPONSE" | jq -r '.id')

echo ""
echo "✅ Pedido criado com ID: $ORDER_ID"
echo ""

# Aguardar processamento
echo "⏳ Aguardando processamento de eventos (5 segundos)..."
sleep 5

# Consultar pedido
echo "🔍 Consultando pedido..."
curl -s http://localhost:5000/api/orders/$ORDER_ID | jq '.'

echo ""
echo "✅ Teste completo finalizado!"
```

Use assim:
```bash
chmod +x test_complete_flow.sh
./test_complete_flow.sh
```

## 🐛 Debug e Troubleshooting

### Verificar se RabbitMQ está respondendo

```bash
curl -i http://localhost:15672/api/aliveness-test/% -u guest:guest
```

### Ver logs de um serviço específico

```bash
# No terminal do serviço, pressione Ctrl+C para parar e ver todos os logs
# Ou redirecione para arquivo:
dotnet run > payment-service.log 2>&1
```

### Listar conexões RabbitMQ

```bash
docker exec order-processing-rabbitmq rabbitmq-diagnostics list_connections
```

### Limpar todas as mensagens de uma fila

```bash
docker exec order-processing-rabbitmq \
  rabbitmqctl purge_queue payment-queue
```

## ✅ Checklist de Validação

- [ ] Health check retorna 200
- [ ] Criar pedido retorna 201 com ID
- [ ] Pedido criado dispara eventos em todos os serviços
- [ ] Payment Service processa e publica evento
- [ ] Inventory Service recebe e processa
- [ ] Notification Service envia para todos os eventos
- [ ] Listar pedidos retorna o pedido criado
- [ ] Consultar pedido específico retorna dados corretos
- [ ] Erros retornam status HTTP correto (400, 404, etc)
- [ ] RabbitMQ Management UI mostra filas com mensagens

## 📈 Teste de Carga (Opcional)

Para testar com múltiplos pedidos simultâneos:

```bash
#!/bin/bash

echo "🔥 Iniciando teste de carga..."

for i in {1..10}; do
  curl -s -X POST http://localhost:5000/api/orders \
    -H "Content-Type: application/json" \
    -d "{
      \"customerId\": \"550e8400-e29b-41d4-a716-44665544000$((i % 10))\",
      \"items\": [
        {
          \"productId\": \"00000000-0000-0000-0000-00000000000$((i % 3 + 1))\",
          \"quantity\": $((i % 5 + 1)),
          \"price\": $((50 + i * 10))
        }
      ]
    }" &
done

wait
echo "✅ 10 pedidos criados!"
curl -s http://localhost:5000/api/orders | jq 'length'
```

---

**Bom teste! 🚀**

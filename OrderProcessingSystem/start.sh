#!/bin/bash

# Order Processing System - Startup Script
# Este script inicia todos os componentes do sistema

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Order Processing System - Startup                     ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

# Verificar se Docker está instalado
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker não está instalado!${NC}"
    echo "    Instale Docker em: https://www.docker.com/products/docker-desktop"
    exit 1
fi

# Verificar se Docker Compose está instalado
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}❌ Docker Compose não está instalado!${NC}"
    exit 1
fi

# Verificar se .NET SDK está instalado
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK não está instalado!${NC}"
    echo "    Instale em: https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "${GREEN}✅ Dependências encontradas${NC}"
echo ""

# Paso 1: Inicia RabbitMQ
echo -e "${YELLOW}📦 [1/4] Iniciando RabbitMQ...${NC}"

if docker-compose ps | grep -q "order-processing-rabbitmq"; then
    echo -e "${YELLOW}⚠️  RabbitMQ já está rodando${NC}"
else
    docker-compose up -d
    echo -e "${YELLOW}⏳ Aguardando RabbitMQ ficar pronto...${NC}"
    
    # Aguardar RabbitMQ estar pronto
    for i in {1..30}; do
        if docker-compose exec rabbitmq rabbitmq-diagnostics -q ping > /dev/null 2>&1; then
            echo -e "${GREEN}✅ RabbitMQ está pronto!${NC}"
            break
        fi
        echo -ne "   Tentativa $i/30...\r"
        sleep 1
    done
fi

echo ""

# Paso 2: Build da solução
echo -e "${YELLOW}🔨 [2/4] Fazendo build da solução...${NC}"
dotnet build > /dev/null 2>&1
echo -e "${GREEN}✅ Build concluído${NC}"
echo ""

# Paso 3: Mostrar instrções para iniciar serviços
echo -e "${YELLOW}🚀 [3/4] Pronto para iniciar os serviços!${NC}"
echo ""
echo -e "${BLUE}Abra 4 terminais separados e execute:${NC}"
echo ""
echo -e "${GREEN}Terminal 1 (Order API):${NC}"
echo -e "  ${BLUE}cd src/OrderApi && dotnet run${NC}"
echo ""
echo -e "${GREEN}Terminal 2 (Payment Service):${NC}"
echo -e "  ${BLUE}cd src/PaymentService && dotnet run${NC}"
echo ""
echo -e "${GREEN}Terminal 3 (Inventory Service):${NC}"
echo -e "  ${BLUE}cd src/InventoryService && dotnet run${NC}"
echo ""
echo -e "${GREEN}Terminal 4 (Notification Service):${NC}"
echo -e "  ${BLUE}cd src/NotificationService && dotnet run${NC}"
echo ""

# Paso 4: Mostrar links úteis
echo -e "${YELLOW}🔗 [4/4] Links úteis:${NC}"
echo ""
echo -e "  📊 RabbitMQ Dashboard: ${BLUE}http://localhost:15672${NC}"
echo -e "     Usuário: guest | Senha: guest"
echo ""
echo -e "  🔍 API Swagger: ${BLUE}http://localhost:5000/swagger${NC}"
echo ""
echo -e "  📚 Documentação: ${BLUE}QUICKSTART.md${NC}, ${BLUE}README.md${NC}, ${BLUE}TESTING.md${NC}"
echo ""

# Mostrar status do RabbitMQ
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}RabbitMQ Status:${NC}"
docker-compose ps

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${GREEN}✅ Sistema pronto!${NC}"
echo ""
echo "Próximos passos:"
echo "1. Abra os 4 terminais listados acima"
echo "2. Acesse http://localhost:5000/swagger"
echo "3. Crie um pedido via POST /api/orders"
echo "4. Observe os logs de todos os serviços"
echo ""
echo "Para testar com curl, veja TESTING.md"
echo ""

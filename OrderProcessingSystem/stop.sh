#!/bin/bash

# Order Processing System - Cleanup Script
# Este script para todos os serviços e limpa o sistema

echo "🛑 Parando Order Processing System..."
echo ""

# Parar RabbitMQ
echo "📦 Parando RabbitMQ..."
docker-compose down
echo "✅ RabbitMQ parado"

echo ""
echo "🧹 Limpeza concluída!"
echo ""
echo "Para remover volumes de dados também, execute:"
echo "  docker-compose down -v"
echo ""

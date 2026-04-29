using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

namespace InventoryService;

/// <summary>
/// Worker que consome eventos de pagamento aprovado e processa reserva de estoque
/// </summary>
public class InventoryProcessingWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryProcessingWorker> _logger;
    private IEventConsumer? _eventConsumer;

    public InventoryProcessingWorker(
        IConnection connection,
        IInventoryService inventoryService,
        ILogger<InventoryProcessingWorker> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Inventory Service iniciado");

        try
        {
            _eventConsumer = new RabbitMqEventConsumer(
                _connection,
                queueName: "inventory-queue",
                routingKey: "payment.processed",
                exchangeName: "order-exchange"
            );

            await _eventConsumer.ConsumeAsync<PaymentProcessedEvent>(
                "inventory-queue",
                async @event =>
                {
                    try
                    {
                        _logger.LogInformation("💳 Evento recebido: PaymentProcessedEvent | OrderId: {@OrderId} | Status: {@Status}", 
                            @event.OrderId, @event.Status);

                        // Apenas processar se pagamento foi aprovado
                        if (@event.Status.ToLower() == "approved")
                        {
                            // Para este exemplo, vamos simular itens do pedido
                            // Em um sistema real, teríamos acesso aos dados do pedido
                            var mockItems = new List<OrderProcessingSystem.Shared.OrderItem>
                            {
                                new(Guid.Parse("00000000-0000-0000-0000-000000000001"), 2, 50)
                            };

                            var reserved = await _inventoryService.ReserveInventoryAsync(@event.OrderId, mockItems);

                            var inventoryStatus = reserved ? "reserved" : "insufficient";
                            var inventoryEvent = new InventoryReservedEvent(@event.OrderId, inventoryStatus);

                            await PublishInventoryEvent(inventoryEvent);
                        }
                        else
                        {
                            _logger.LogWarning("⏭️ Pulando processamento de estoque - pagamento rejeitado");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao processar estoque para pedido {OrderId}", @event.OrderId);
                        throw;
                    }
                }
            );

            await _eventConsumer.StartConsumingAsync();

            // Manter o worker rodando
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 Inventory Service cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro fatal no Inventory Service");
        }
    }

    private async Task PublishInventoryEvent(InventoryReservedEvent @event)
    {
        try
        {
            var publisher = new RabbitMqEventPublisher(_connection, "order-exchange");
            await publisher.PublishAsync(@event, "inventory.reserved");
            publisher.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao publicar evento de estoque");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Encerrando Inventory Service...");
        _eventConsumer?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}

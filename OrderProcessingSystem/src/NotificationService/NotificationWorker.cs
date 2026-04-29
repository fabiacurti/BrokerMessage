using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

namespace NotificationService;

/// <summary>
/// Worker que consome todos os eventos e envia notificações
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        IConnection connection,
        INotificationSender notificationSender,
        ILogger<NotificationWorker> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Notification Service iniciado");

        try
        {
            // Consumer para OrderCreatedEvent
            _ = Task.Run(async () => await ConsumeOrderCreatedEventsAsync(stoppingToken), stoppingToken);

            // Consumer para PaymentProcessedEvent
            _ = Task.Run(async () => await ConsumePaymentProcessedEventsAsync(stoppingToken), stoppingToken);

            // Consumer para InventoryReservedEvent
            _ = Task.Run(async () => await ConsumeInventoryReservedEventsAsync(stoppingToken), stoppingToken);

            // Manter o worker rodando
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 Notification Service cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro fatal no Notification Service");
        }
    }

    private async Task ConsumeOrderCreatedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var consumer = new RabbitMqEventConsumer(
                _connection,
                queueName: "notification-queue-order",
                routingKey: "order.created",
                exchangeName: "order-exchange"
            );

            await consumer.ConsumeAsync<OrderCreatedEvent>(
                "notification-queue-order",
                async @event =>
                {
                    try
                    {
                        await _notificationSender.SendOrderCreatedNotificationAsync(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao enviar notificação de pedido");
                        throw;
                    }
                }
            );

            await consumer.StartConsumingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no consumidor de OrderCreatedEvent");
        }
    }

    private async Task ConsumePaymentProcessedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var consumer = new RabbitMqEventConsumer(
                _connection,
                queueName: "notification-queue-payment",
                routingKey: "payment.processed",
                exchangeName: "order-exchange"
            );

            await consumer.ConsumeAsync<PaymentProcessedEvent>(
                "notification-queue-payment",
                async @event =>
                {
                    try
                    {
                        await _notificationSender.SendPaymentNotificationAsync(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao enviar notificação de pagamento");
                        throw;
                    }
                }
            );

            await consumer.StartConsumingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no consumidor de PaymentProcessedEvent");
        }
    }

    private async Task ConsumeInventoryReservedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var consumer = new RabbitMqEventConsumer(
                _connection,
                queueName: "notification-queue-inventory",
                routingKey: "inventory.reserved",
                exchangeName: "order-exchange"
            );

            await consumer.ConsumeAsync<InventoryReservedEvent>(
                "notification-queue-inventory",
                async @event =>
                {
                    try
                    {
                        await _notificationSender.SendInventoryNotificationAsync(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao enviar notificação de estoque");
                        throw;
                    }
                }
            );

            await consumer.StartConsumingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no consumidor de InventoryReservedEvent");
        }
    }
}

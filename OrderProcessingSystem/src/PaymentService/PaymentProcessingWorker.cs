using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

namespace PaymentService;

/// <summary>
/// Worker que consome eventos de pedidos criados e processa pagamentos
/// </summary>
public class PaymentProcessingWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly ILogger<PaymentProcessingWorker> _logger;
    private IEventConsumer? _eventConsumer;

    public PaymentProcessingWorker(
        IConnection connection,
        IPaymentProcessor paymentProcessor,
        ILogger<PaymentProcessingWorker> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _paymentProcessor = paymentProcessor ?? throw new ArgumentNullException(nameof(paymentProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Payment Service iniciado");

        try
        {
            _eventConsumer = new RabbitMqEventConsumer(
                _connection,
                queueName: "payment-queue",
                routingKey: "order.created",
                exchangeName: "order-exchange"
            );

            await _eventConsumer.ConsumeAsync<OrderCreatedEvent>(
                "payment-queue",
                async @event =>
                {
                    try
                    {
                        _logger.LogInformation("📦 Evento recebido: OrderCreatedEvent | OrderId: {@OrderId}", @event.OrderId);
                        
                        // Processar pagamento
                        await _paymentProcessor.ProcessPaymentAsync(@event.OrderId, @event.Total);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao processar pedido {OrderId}", @event.OrderId);
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
            _logger.LogInformation("🛑 Payment Service cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro fatal no Payment Service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Encerrando Payment Service...");
        _eventConsumer?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}

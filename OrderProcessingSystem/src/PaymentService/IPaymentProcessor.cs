using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderProcessingSystem.Shared;

namespace PaymentService;

/// <summary>
/// Interface do serviço de processamento de pagamentos
/// </summary>
public interface IPaymentProcessor
{
    Task<bool> ProcessPaymentAsync(Guid orderId, decimal amount);
}

/// <summary>
/// Simulador de processamento de pagamentos
/// </summary>
public class MockPaymentProcessor : IPaymentProcessor
{
    private static readonly Random Random = new();
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<MockPaymentProcessor> _logger;

    public MockPaymentProcessor(IEventPublisher eventPublisher, ILogger<MockPaymentProcessor> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ProcessPaymentAsync(Guid orderId, decimal amount)
    {
        _logger.LogInformation("💳 Processando pagamento para pedido {OrderId} | Valor: {Amount}", orderId, amount);

        // Simular delay do processamento
        await Task.Delay(Random.Next(500, 2000));

        // Simular 80% de aprovação, 20% de rejeição
        var isApproved = Random.Next(100) < 80;

        var status = isApproved ? "approved" : "rejected";

        _logger.LogInformation("💰 Pagamento {Status} para pedido {OrderId}", status.ToUpper(), orderId);

        // Publicar evento
        var @event = new PaymentProcessedEvent(orderId, status);
        await _eventPublisher.PublishAsync(@event, "payment.processed");

        return isApproved;
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderProcessingSystem.Shared;

namespace NotificationService;

/// <summary>
/// Interface para envio de notificações
/// </summary>
public interface INotificationSender
{
    Task SendOrderCreatedNotificationAsync(OrderCreatedEvent @event);
    Task SendPaymentNotificationAsync(PaymentProcessedEvent @event);
    Task SendInventoryNotificationAsync(InventoryReservedEvent @event);
}

/// <summary>
/// Simulador de envio de notificações (email/SMS)
/// </summary>
public class MockNotificationSender : INotificationSender
{
    private readonly ILogger<MockNotificationSender> _logger;

    public MockNotificationSender(ILogger<MockNotificationSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendOrderCreatedNotificationAsync(OrderCreatedEvent @event)
    {
        await Task.Delay(100);
        
        _logger.LogInformation("📧 [NOTIFICAÇÃO] Pedido criado");
        _logger.LogInformation("   - OrderId: {@OrderId}", @event.OrderId);
        _logger.LogInformation("   - CustomerId: {@CustomerId}", @event.CustomerId);
        _logger.LogInformation("   - Total: R${Total:F2}", @event.Total);
        _logger.LogInformation("   - [EMAIL ENVIADO] Obrigado por sua compra!");
    }

    public async Task SendPaymentNotificationAsync(PaymentProcessedEvent @event)
    {
        await Task.Delay(100);

        if (@event.Status.ToLower() == "approved")
        {
            _logger.LogInformation("📧 [NOTIFICAÇÃO] Pagamento aprovado");
            _logger.LogInformation("   - OrderId: {@OrderId}", @event.OrderId);
            _logger.LogInformation("   - [EMAIL ENVIADO] Seu pagamento foi confirmado!");
        }
        else
        {
            _logger.LogWarning("📧 [NOTIFICAÇÃO] Pagamento rejeitado");
            _logger.LogWarning("   - OrderId: {@OrderId}", @event.OrderId);
            _logger.LogWarning("   - [EMAIL ENVIADO] Desculpe, seu pagamento foi rejeitado. Tente novamente.");
        }
    }

    public async Task SendInventoryNotificationAsync(InventoryReservedEvent @event)
    {
        await Task.Delay(100);

        if (@event.Status.ToLower() == "reserved")
        {
            _logger.LogInformation("📧 [NOTIFICAÇÃO] Estoque reservado");
            _logger.LogInformation("   - OrderId: {@OrderId}", @event.OrderId);
            _logger.LogInformation("   - [EMAIL ENVIADO] Seu pedido foi confirmado e será processado!");
        }
        else
        {
            _logger.LogWarning("📧 [NOTIFICAÇÃO] Estoque insuficiente");
            _logger.LogWarning("   - OrderId: {@OrderId}", @event.OrderId);
            _logger.LogWarning("   - [EMAIL ENVIADO] Desculpe, alguns itens não estão disponíveis.");
        }
    }
}

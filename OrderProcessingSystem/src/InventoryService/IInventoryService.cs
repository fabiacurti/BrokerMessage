using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderProcessingSystem.Shared;

namespace InventoryService;

/// <summary>
/// Interface do serviço de estoque
/// </summary>
public interface IInventoryService
{
    Task<bool> ReserveInventoryAsync(Guid orderId, List<OrderProcessingSystem.Shared.OrderItem> items);
}

/// <summary>
/// Serviço de gerenciamento de estoque simulado
/// </summary>
public class MockInventoryService : IInventoryService
{
    private static readonly Dictionary<Guid, int> Stock = new();
    private static readonly object LockObject = new();
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<MockInventoryService> _logger;

    public MockInventoryService(IEventPublisher eventPublisher, ILogger<MockInventoryService> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Inicializar estoque simulado
        InitializeStock();
    }

    private void InitializeStock()
    {
        lock (LockObject)
        {
            // Simular alguns produtos com estoque
            Stock[Guid.Parse("00000000-0000-0000-0000-000000000001")] = 100;
            Stock[Guid.Parse("00000000-0000-0000-0000-000000000002")] = 50;
            Stock[Guid.Parse("00000000-0000-0000-0000-000000000003")] = 75;
        }
    }

    public async Task<bool> ReserveInventoryAsync(Guid orderId, List<OrderProcessingSystem.Shared.OrderItem> items)
    {
        _logger.LogInformation("📦 Processando reserva de estoque para pedido {OrderId}", orderId);

        await Task.Delay(500); // Simular delay

        lock (LockObject)
        {
            // Verificar se há estoque suficiente
            var allItemsAvailable = items.All(item =>
            {
                var hasStock = Stock.TryGetValue(item.ProductId, out var quantity);
                return hasStock && quantity >= item.Quantity;
            });

            if (allItemsAvailable)
            {
                // Reservar estoque
                foreach (var item in items)
                {
                    if (Stock.ContainsKey(item.ProductId))
                    {
                        Stock[item.ProductId] -= item.Quantity;
                        _logger.LogInformation("✅ Estoque reservado | Produto: {ProductId} | Qtd: {Quantity}", 
                            item.ProductId, item.Quantity);
                    }
                }

                return true;
            }

            _logger.LogWarning("⚠️ Estoque insuficiente para pedido {OrderId}", orderId);
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderProcessingSystem.Shared;

namespace OrderApi;

/// <summary>
/// Interface do serviço de pedidos
/// </summary>
public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> GetOrderAsync(Guid orderId);
    Task<List<OrderResponse>> GetAllOrdersAsync();
    Task UpdateOrderStatusAsync(Guid orderId, string status);
}

/// <summary>
/// Implementação do serviço de pedidos
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public OrderService(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        // Validar request
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId é obrigatório");

        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("Pedido deve ter pelo menos um item");

        // Calcular total
        var total = request.Items.Sum(item => item.Price * item.Quantity);

        // Criar pedido
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items,
            Total = total,
            Status = "pending"
        };

        var createdOrder = await _orderRepository.CreateAsync(order);

        // Publicar evento
        var orderCreatedEvent = new OrderCreatedEvent(
            createdOrder.Id,
            createdOrder.CustomerId,
            createdOrder.Items.ConvertAll(item => new OrderProcessingSystem.Shared.OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }),
            createdOrder.Total
        );

        await _eventPublisher.PublishAsync(orderCreatedEvent, "order.created");

        return MapToResponse(createdOrder);
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return MapToResponse(order);
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToResponse).ToList();
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        order.Status = status;
        await _orderRepository.UpdateAsync(order);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items,
            Total = order.Total,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        };
    }
}

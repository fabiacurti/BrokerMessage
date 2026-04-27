using System;
using System.Collections.Generic;

namespace OrderProcessingSystem.Shared;

/// <summary>
/// Evento base para todos os eventos do sistema
/// </summary>
public abstract class IntegrationEvent
{
    public Guid EventId { get; }
    public DateTime CreatedAt { get; }

    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected IntegrationEvent(Guid eventId, DateTime createdAt)
    {
        EventId = eventId;
        CreatedAt = createdAt;
    }
}

/// <summary>
/// Evento publicado quando um pedido é criado
/// </summary>
public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Total { get; set; }

    public OrderCreatedEvent() { }

    public OrderCreatedEvent(Guid orderId, Guid customerId, List<OrderItem> items, decimal total)
        : base()
    {
        OrderId = orderId;
        CustomerId = customerId;
        Items = items;
        Total = total;
    }
}

/// <summary>
/// Evento publicado quando o pagamento é processado
/// </summary>
public class PaymentProcessedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } // "approved" ou "rejected"
    public DateTime ProcessedAt { get; set; }

    public PaymentProcessedEvent() { }

    public PaymentProcessedEvent(Guid orderId, string status)
        : base()
    {
        OrderId = orderId;
        Status = status;
        ProcessedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Evento publicado quando o estoque é reservado
/// </summary>
public class InventoryReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } // "reserved" ou "insufficient"
    public DateTime ReservedAt { get; set; }

    public InventoryReservedEvent() { }

    public InventoryReservedEvent(Guid orderId, string status)
        : base()
    {
        OrderId = orderId;
        Status = status;
        ReservedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Modelo de item do pedido
/// </summary>
public class OrderItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public OrderItem() { }

    public OrderItem(Guid productId, int quantity, decimal price)
    {
        ProductId = productId;
        Quantity = quantity;
        Price = price;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderApi;

/// <summary>
/// Interface do repositório de pedidos
/// </summary>
public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order> GetByIdAsync(Guid id);
    Task<Order> UpdateAsync(Order order);
    Task<List<Order>> GetAllAsync();
}

/// <summary>
/// Implementação em memória do repositório de pedidos
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
    private static readonly Dictionary<Guid, Order> Orders = new();
    private static readonly object LockObject = new();

    public Task<Order> CreateAsync(Order order)
    {
        lock (LockObject)
        {
            order.Id = Guid.NewGuid();
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            Orders[order.Id] = order;

            Console.WriteLine($"📝 Pedido criado em memória: {order.Id}");
            return Task.FromResult(order);
        }
    }

    public Task<Order> GetByIdAsync(Guid id)
    {
        lock (LockObject)
        {
            if (Orders.TryGetValue(id, out var order))
                return Task.FromResult(order);

            throw new KeyNotFoundException($"Pedido {id} não encontrado");
        }
    }

    public Task<Order> UpdateAsync(Order order)
    {
        lock (LockObject)
        {
            if (!Orders.ContainsKey(order.Id))
                throw new KeyNotFoundException($"Pedido {order.Id} não encontrado");

            order.UpdatedAt = DateTime.UtcNow;
            Orders[order.Id] = order;

            Console.WriteLine($"🔄 Pedido atualizado: {order.Id} -> Status: {order.Status}");
            return Task.FromResult(order);
        }
    }

    public Task<List<Order>> GetAllAsync()
    {
        lock (LockObject)
        {
            return Task.FromResult(Orders.Values.ToList());
        }
    }
}

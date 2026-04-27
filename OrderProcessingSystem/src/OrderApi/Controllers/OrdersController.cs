using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Controllers;

/// <summary>
/// Controller para API de pedidos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cria um novo pedido
    /// </summary>
    /// <param name="request">Dados do pedido</param>
    /// <returns>Pedido criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("📋 Criando novo pedido para cliente: {CustomerId}", request.CustomerId);
            
            var order = await _orderService.CreateOrderAsync(request);
            
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("⚠️ Erro de validação: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Erro ao criar pedido: {Message}", ex.Message);
            return StatusCode(500, new { error = "Erro ao criar pedido" });
        }
    }

    /// <summary>
    /// Obtém um pedido específico
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Detalhes do pedido</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("🔍 Consultando pedido: {OrderId}", id);
            
            var order = await _orderService.GetOrderAsync(id);
            
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("⚠️ Pedido não encontrado: {OrderId}", id);
            return NotFound(new { error = "Pedido não encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Erro ao obter pedido: {Message}", ex.Message);
            return StatusCode(500, new { error = "Erro ao obter pedido" });
        }
    }

    /// <summary>
    /// Obtém todos os pedidos
    /// </summary>
    /// <returns>Lista de pedidos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            _logger.LogInformation("📋 Consultando todos os pedidos");
            
            var orders = await _orderService.GetAllOrdersAsync();
            
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Erro ao obter pedidos: {Message}", ex.Message);
            return StatusCode(500, new { error = "Erro ao obter pedidos" });
        }
    }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "Order API" });
    }
}

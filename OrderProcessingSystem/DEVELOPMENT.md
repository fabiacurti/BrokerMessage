# 👨‍💻 Guia de Desenvolvimento

## 🏗️ Estrutura de Projeto para Novos Desenvolvedores

### Antes de Começar

1. Clone/acesse o repositório
2. Instale [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
3. Instale [Docker](https://www.docker.com/products/docker-desktop)
4. Leia [INDEX.md](INDEX.md) para entender a estrutura

### Setup Inicial

```bash
# 1. Restaurar dependências
dotnet restore

# 2. Build da solução
dotnet build

# 3. Iniciar RabbitMQ
docker-compose up -d

# 4. Verificar RabbitMQ
curl http://localhost:15672/api/aliveness-test/% -u guest:guest
```

## 📝 Adicionando Novos Eventos

### Passo 1: Criar a classe de evento em `src/Shared/Events.cs`

```csharp
public class OrderShippedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public DateTime ShippedAt { get; set; }
    public string TrackingNumber { get; set; }

    public OrderShippedEvent() { }

    public OrderShippedEvent(Guid orderId, string trackingNumber)
        : base()
    {
        OrderId = orderId;
        ShippedAt = DateTime.UtcNow;
        TrackingNumber = trackingNumber;
    }
}
```

### Passo 2: Publicar o evento

**Exemplo em OrderService:**

```csharp
// Após atualizar status do pedido para shipped
var shippedEvent = new OrderShippedEvent(
    orderId: order.Id,
    trackingNumber: "TRACK123456"
);

await _eventPublisher.PublishAsync(shippedEvent, "order.shipped");
```

### Passo 3: Consumir o evento em um serviço

**Exemplo de novo ShippingService:**

```csharp
public class ShippingProcessingWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<ShippingProcessingWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new RabbitMqEventConsumer(
            _connection,
            queueName: "shipping-queue",
            routingKey: "order.shipped"
        );

        await consumer.ConsumeAsync<OrderShippedEvent>(
            "shipping-queue",
            async @event =>
            {
                _logger.LogInformation("Processando envio: {OrderId}", @event.OrderId);
                // Lógica de shipping aqui
            }
        );

        await consumer.StartConsumingAsync();
    }
}
```

## 🆕 Criando um Novo Worker Service

### Template Básico

```bash
mkdir src/NewService
cd src/NewService
dotnet new worker --force
```

### Arquivo: `Program.cs`

```csharp
using NewService;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var rabbitMqHost = context.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = int.Parse(context.Configuration["RabbitMQ:Port"] ?? "5672");

        var connectionFactory = new ConnectionFactory
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        var connection = connectionFactory.CreateConnectionAsync().Result;
        services.AddSingleton(connection);

        // Seus serviços aqui
        services.AddScoped<IYourService, YourServiceImplementation>();

        // Seu worker
        services.AddHostedService<YourProcessingWorker>();
    })
    .Build();

await host.RunAsync();
```

### Arquivo: `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Shared/Shared.csproj" />
  </ItemGroup>

</Project>
```

## 🔄 Modificando a Order API

### Adicionar novo endpoint

**Em `Controllers/OrdersController.cs`:**

```csharp
[HttpPut("{id}/cancel")]
public async Task<IActionResult> CancelOrder(Guid id)
{
    try
    {
        var order = await _orderService.GetOrderAsync(id);
        
        if (order.Status != "pending")
            return BadRequest(new { error = "Apenas pedidos pendentes podem ser cancelados" });

        await _orderService.UpdateOrderStatusAsync(id, "cancelled");

        // Publicar evento de cancelamento
        var cancelledEvent = new OrderCancelledEvent(id);
        // ... publicar

        return Ok(new { message = "Pedido cancelado" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao cancelar pedido");
        return StatusCode(500);
    }
}
```

### Adicionar novo serviço

**Criar `Services/NewService.cs`:**

```csharp
public interface INewService
{
    Task DoSomethingAsync(Guid orderId);
}

public class NewService : INewService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<NewService> _logger;

    public NewService(
        IOrderRepository orderRepository,
        IEventPublisher eventPublisher,
        ILogger<NewService> logger)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task DoSomethingAsync(Guid orderId)
    {
        // Implementação aqui
        _logger.LogInformation("Fazendo algo com pedido {OrderId}", orderId);
    }
}
```

**Registrar em `Program.cs`:**

```csharp
builder.Services.AddScoped<INewService, NewService>();
```

## 🧪 Testes Unitários

### Exemplo com xUnit

**Criar `OrderServiceTests.cs`:**

```csharp
using Xunit;
using Moq;
using OrderApi;
using OrderProcessingSystem.Shared;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_ValidRequest_CreatesOrderAndPublishesEvent()
    {
        // Arrange
        var mockRepository = new Mock<IOrderRepository>();
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockRepository.Object, mockPublisher.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<OrderItemDto>
            {
                new(Guid.NewGuid(), 2, 50m)
            }
        };

        var createdOrder = new Order { Id = Guid.NewGuid() };
        mockRepository.Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await service.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
        mockRepository.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
        mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), "order.created"), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyItems_ThrowsException()
    {
        // Arrange
        var mockRepository = new Mock<IOrderRepository>();
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockRepository.Object, mockPublisher.Object);

        var request = new CreateOrderRequest { CustomerId = Guid.NewGuid(), Items = new() };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOrderAsync(request));
    }
}
```

## 📊 Melhorando o Repositório

### Usar Entity Framework Core

**Install:**
```bash
dotnet add OrderApi package Microsoft.EntityFrameworkCore.SqlServer
```

**Criar `Data/OrderDbContext.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;

namespace OrderApi.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasKey(o => o.Id);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasDefaultValue("pending");
    }
}
```

**Criar `Repositories/SqlOrderRepository.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;

namespace OrderApi.Data;

public class SqlOrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public SqlOrderRepository(OrderDbContext context) => _context = context;

    public async Task<Order> CreateAsync(Order order)
    {
        order.Id = Guid.NewGuid();
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<Order> GetByIdAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            throw new KeyNotFoundException($"Pedido {id} não encontrado");
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders.ToListAsync();
    }
}
```

## 🚀 Deploy em Desenvolvimento

### Usar launchSettings.json para múltiplos perfis

**`OrderApi/Properties/launchSettings.json`:**

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "docker": {
      "commandName": "Docker",
      "applicationUrl": "http://localhost:5000"
    }
  }
}
```

### Dockerfile para worker

**`PaymentService/Dockerfile`:**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/PaymentService/PaymentService.csproj", "src/PaymentService/"]

RUN dotnet restore "src/PaymentService/PaymentService.csproj"

COPY . .
RUN dotnet build "src/PaymentService/PaymentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/PaymentService/PaymentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
```

## 🔍 Debugging

### Debugar com VS Code

**`.vscode/launch.json`:**

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Order API",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/OrderApi/bin/Debug/net8.0/OrderApi.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/OrderApi",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

## 📚 Referências

- [ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [xUnit](https://xunit.net/)
- [RabbitMQ .NET Client](https://github.com/rabbitmq/rabbitmq-dotnet-client)

## ✅ Checklist para Contribuir

- [ ] Código segue padrões do projeto
- [ ] Adiciona logging apropriado
- [ ] Usa async/await
- [ ] Implementa error handling
- [ ] Testes unitários se aplicável
- [ ] Documentação atualizada
- [ ] Sem breaking changes (se possível)

---

**Bom desenvolvimento! 🚀**

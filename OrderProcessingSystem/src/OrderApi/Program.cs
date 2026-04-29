using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderApi;
using OrderApi.Controllers;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Adicionar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar RabbitMQ
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");

var connectionFactory = new ConnectionFactory
{
    HostName = rabbitMqHost,
    Port = rabbitMqPort,
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    RequestedHeartbeat = TimeSpan.FromSeconds(30)
};

Console.WriteLine($"🐰 Conectando ao RabbitMQ: {rabbitMqHost}:{rabbitMqPort}");

var connection = connectionFactory.CreateConnectionAsync().Result;
builder.Services.AddSingleton(connection);

// Registrar serviços de domínio
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEventPublisher>(sp => 
    new RabbitMqEventPublisher(sp.GetRequiredService<IConnection>(), "order-exchange"));

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("🛑 Encerrando Order API...");
    connection?.Dispose();
});

Console.WriteLine("✅ Order API iniciada!");
await app.RunAsync();

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InventoryService;
using OrderProcessingSystem.Shared;
using RabbitMQ.Client;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configurar logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Configurar RabbitMQ
        var rabbitMqHost = context.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = int.Parse(context.Configuration["RabbitMQ:Port"] ?? "5672");

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
        services.AddSingleton(connection);

        // Registrar serviços
        services.AddScoped<IInventoryService, MockInventoryService>();
        services.AddScoped<IEventPublisher>(sp =>
            new RabbitMqEventPublisher(sp.GetRequiredService<IConnection>(), "order-exchange"));

        // Registrar worker
        services.AddHostedService<InventoryProcessingWorker>();
    })
    .Build();

Console.WriteLine("✅ Inventory Service iniciado!");
await host.RunAsync();

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderProcessingSystem.Shared;

/// <summary>
/// Interface para publicação de eventos
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent;
}

/// <summary>
/// Interface para consumo de eventos
/// </summary>
public interface IEventConsumer
{
    Task ConsumeAsync<T>(string queueName, Func<T, Task> handler) where T : IntegrationEvent;
    Task StartConsumingAsync();
    void Dispose();
}

/// <summary>
/// Implementação de publicador de eventos via RabbitMQ
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private IChannel? _channel;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqEventPublisher(IConnection connection, string exchangeName = "order-exchange")
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _exchangeName = exchangeName;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        InitializeExchange();
    }

    private void InitializeExchange()
    {
        try
        {
            _channel = _connection.CreateChannelAsync().Result;
            _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            ).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar exchange: {ex.Message}");
            throw;
        }
    }

    public async Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        if (_channel == null)
            throw new InvalidOperationException("Canal de comunicação não foi inicializado");

        try
        {
            var message = JsonSerializer.Serialize(@event, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: new ReadOnlyMemory<byte>(body)
            );

            Console.WriteLine($"✅ Evento publicado: {typeof(T).Name} | Routing Key: {routingKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao publicar evento: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

/// <summary>
/// Implementação de consumidor de eventos via RabbitMQ
/// </summary>
public class RabbitMqEventConsumer : IEventConsumer, IDisposable
{
    private readonly IConnection _connection;
    private IChannel? _channel;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly JsonSerializerOptions _jsonOptions;
    private AsyncEventingBasicConsumer? _consumer;

    public RabbitMqEventConsumer(
        IConnection connection,
        string queueName,
        string routingKey,
        string exchangeName = "order-exchange")
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
        _exchangeName = exchangeName;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task ConsumeAsync<T>(string queueName, Func<T, Task> handler) where T : IntegrationEvent
    {
        try
        {
            _channel = _connection.CreateChannelAsync().Result;

            // Configurar exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Configurar fila
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Fazer binding
            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: _routingKey
            );

            // Configurar QoS (1 mensagem por vez)
            await _channel.BasicQosAsync(0, 1, false);

            // Criar consumer
            _consumer = new AsyncEventingBasicConsumer(_channel);

            _consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<T>(message, _jsonOptions);

                    if (@event != null)
                    {
                        await handler(@event);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        Console.WriteLine($"✅ Evento processado: {typeof(T).Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao processar evento: {ex.Message}");
                    // Rejeitar e retornar para fila (retry)
                    if (_channel != null)
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: _consumer
            );

            Console.WriteLine($"Começando a consumir de {queueName} com routing key {_routingKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao configurar consumer: {ex.Message}");
            throw;
        }
    }

    public async Task StartConsumingAsync()
    {
        // Consumer já está rodando após o BasicConsumeAsync
        await Task.Delay(1000); // Pequeno delay para garantir que tudo está configurado
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

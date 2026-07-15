using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Events;
using Shared.Messages;

namespace PaymentService.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(EventTypes.PaymentExchange, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(EventTypes.PaymentProcessedQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(EventTypes.ShippingQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(EventTypes.NotificationQueue, durable: true, exclusive: false, autoDelete: false);

        _channel.QueueBind(EventTypes.PaymentProcessedQueue, EventTypes.PaymentExchange, EventTypes.PaymentProcessedQueue);
        _channel.QueueBind(EventTypes.ShippingQueue, EventTypes.PaymentExchange, EventTypes.ShippingQueue);
        _channel.QueueBind(EventTypes.NotificationQueue, EventTypes.PaymentExchange, EventTypes.NotificationQueue);
    }

    public void PublishPaymentProcessed(PaymentProcessed message)
    {
        Publish(EventTypes.PaymentExchange, EventTypes.PaymentProcessedQueue, message);
    }

    public void PublishShippingRequested(ShippingRequested message)
    {
        Publish(EventTypes.PaymentExchange, EventTypes.ShippingQueue, message);
    }

    public void PublishNotificationRequested(NotificationRequested message)
    {
        Publish(EventTypes.PaymentExchange, EventTypes.NotificationQueue, message);
    }

    private void Publish<T>(string exchange, string routingKey, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(exchange, routingKey, properties, body);

        _logger.LogInformation("Published {MessageType} to RabbitMQ exchange {Exchange} with routing {RoutingKey}",
            typeof(T).Name, exchange, routingKey);
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
        _channel.Dispose();
        _connection.Dispose();
    }
}

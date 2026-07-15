using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Events;
using Shared.Messages;
using Shared.Observability;
using Shared.Options;

namespace Shared.Messaging;

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IMessageSerializer _serializer;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        IMessageSerializer serializer,
        ILogger<RabbitMqPublisher> logger)
    {
        _serializer = serializer;
        _logger = logger;

        var factory = CreateFactory(options.Value);
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        DeclareTopology(_channel);
    }

    public void PublishPaymentProcessed(PaymentProcessed message)
        => Publish(EventTypes.PaymentProcessedQueue, message);

    public void PublishShippingRequested(ShippingRequested message)
        => Publish(EventTypes.ShippingQueue, message);

    public void PublishNotificationRequested(NotificationRequested message)
        => Publish(EventTypes.NotificationQueue, message);

    private void Publish<T>(string routingKey, T message)
    {
        var json = _serializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Headers = new Dictionary<string, object>
        {
            { CorrelationIdHeader.Name, Encoding.UTF8.GetBytes(CorrelationContext.GetOrCreate()) }
        };

        _channel.BasicPublish(EventTypes.PaymentExchange, routingKey, properties, body);

        _logger.LogInformation(
            "Published {MessageType} to RabbitMQ exchange {Exchange} with routing {RoutingKey}",
            typeof(T).Name,
            EventTypes.PaymentExchange,
            routingKey);
    }

    private static ConnectionFactory CreateFactory(RabbitMqOptions options)
    {
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            DispatchConsumersAsync = options.DispatchConsumersAsync
        };
    }

    private static void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(EventTypes.PaymentExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(EventTypes.DeadLetterExchange, ExchangeType.Direct, durable: true);

        DeclareQueue(channel, EventTypes.PaymentProcessedQueue);
        DeclareQueue(channel, EventTypes.ShippingQueue);
        DeclareQueue(channel, EventTypes.NotificationQueue);
    }

    private static void DeclareQueue(IModel channel, string queueName)
    {
        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", EventTypes.DeadLetterExchange },
            { "x-dead-letter-routing-key", queueName }
        };

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments);
        channel.QueueBind(queueName, EventTypes.PaymentExchange, queueName);

        var deadLetterQueueName = EventTypes.GetDeadLetterQueueName(queueName);
        channel.QueueDeclare(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(deadLetterQueueName, EventTypes.DeadLetterExchange, queueName);
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
        _channel.Dispose();
        _connection.Dispose();
    }
}

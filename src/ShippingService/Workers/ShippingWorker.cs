using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using Shared.Messages;

namespace ShippingService.Workers;

public class ShippingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShippingWorker> _logger;

    public ShippingWorker(IConfiguration configuration, ILogger<ShippingWorker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
    }

    private void StartConsumer(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = false
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(EventTypes.PaymentExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(EventTypes.ShippingQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(EventTypes.ShippingQueue, EventTypes.PaymentExchange, EventTypes.ShippingQueue);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var shipping = JsonSerializer.Deserialize<ShippingRequested>(json);

                if (shipping == null)
                {
                    _logger.LogWarning("Received null shipping message");
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Preparing shipment for order {OrderId}. Tracking: {TrackingNumber}",
                    shipping.OrderId,
                    shipping.TrackingNumber);

                // Имитация работы службы доставки
                Thread.Sleep(500);

                _logger.LogInformation(
                    "Shipment for order {OrderId} dispatched successfully",
                    shipping.OrderId);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing shipping message");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(EventTypes.ShippingQueue, autoAck: false, consumer);

        _logger.LogInformation("Shipping worker started. Listening queue {Queue}", EventTypes.ShippingQueue);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }
        finally
        {
            channel.Close();
            connection.Close();
        }
    }
}

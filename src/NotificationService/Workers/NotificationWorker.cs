using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using Shared.Messages;

namespace NotificationService.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(IConfiguration configuration, ILogger<NotificationWorker> logger)
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
        channel.QueueDeclare(EventTypes.NotificationQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(EventTypes.NotificationQueue, EventTypes.PaymentExchange, EventTypes.NotificationQueue);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var notification = JsonSerializer.Deserialize<NotificationRequested>(json);

                if (notification == null)
                {
                    _logger.LogWarning("Received null notification message");
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Sending email to {Email}: {Subject}",
                    notification.CustomerEmail,
                    notification.Subject);

                // Имитация отправки email
                Thread.Sleep(300);

                _logger.LogInformation(
                    "Email sent for order {OrderId}",
                    notification.OrderId);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification message");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(EventTypes.NotificationQueue, autoAck: false, consumer);

        _logger.LogInformation("Notification worker started. Listening queue {Queue}", EventTypes.NotificationQueue);

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

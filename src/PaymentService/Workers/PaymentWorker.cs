using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using PaymentService.Services;
using Shared.Events;
using Shared.Messages;

namespace PaymentService.Workers;

public class PaymentWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<PaymentWorker> _logger;

    public PaymentWorker(
        IConfiguration configuration,
        IRabbitMqPublisher publisher,
        ILogger<PaymentWorker> logger)
    {
        _configuration = configuration;
        _publisher = publisher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
    }

    private void StartConsumer(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "payment-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_configuration["Kafka:Topic"] ?? EventTypes.OrdersTopic);

        _logger.LogInformation("Payment worker started. Listening Kafka topic {Topic}", EventTypes.OrdersTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var order = JsonSerializer.Deserialize<OrderCreated>(consumeResult.Message.Value);

                    if (order == null)
                    {
                        _logger.LogWarning("Received null order message");
                        continue;
                    }

                    _logger.LogInformation("Processing payment for order {OrderId}", order.OrderId);

                    // Имитация обработки платежа
                    var paymentSuccess = ProcessPayment(order);

                    var paymentProcessed = new PaymentProcessed
                    {
                        OrderId = order.OrderId,
                        CustomerEmail = order.CustomerEmail,
                        Success = paymentSuccess,
                        TransactionId = Guid.NewGuid().ToString("N")[..12]
                    };

                    _publisher.PublishPaymentProcessed(paymentProcessed);

                    if (paymentSuccess)
                    {
                        _publisher.PublishShippingRequested(new ShippingRequested
                        {
                            OrderId = order.OrderId,
                            CustomerEmail = order.CustomerEmail,
                            Product = order.Product,
                            Quantity = order.Quantity,
                            TrackingNumber = $"TRK-{Guid.NewGuid():N}"[..16].ToUpper()
                        });

                        _publisher.PublishNotificationRequested(new NotificationRequested
                        {
                            OrderId = order.OrderId,
                            CustomerEmail = order.CustomerEmail,
                            Subject = "Оплата прошла успешно",
                            Body = $"Заказ {order.OrderId} на сумму {order.Amount:C} успешно оплачен."
                        });
                    }
                    else
                    {
                        paymentProcessed.ErrorMessage = "Insufficient funds";
                        _publisher.PublishNotificationRequested(new NotificationRequested
                        {
                            OrderId = order.OrderId,
                            CustomerEmail = order.CustomerEmail,
                            Subject = "Ошибка оплаты",
                            Body = $"Не удалось оплатить заказ {order.OrderId}. Причина: {paymentProcessed.ErrorMessage}"
                        });
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private static bool ProcessPayment(OrderCreated order)
    {
        // Простая бизнес-логика: заказы на сумму > 10 000 отклоняем для демонстрации ошибок.
        return order.Amount <= 10_000;
    }
}

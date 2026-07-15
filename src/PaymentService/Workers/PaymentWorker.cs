using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Shared.Events;
using Shared.Idempotency;
using Shared.Messages;
using Shared.Messaging;
using Shared.Observability;
using Shared.Options;

namespace PaymentService.Workers;

public class PaymentWorker : BackgroundService
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<PaymentWorker> _logger;

    public PaymentWorker(
        IOptions<KafkaOptions> kafkaOptions,
        IRabbitMqPublisher publisher,
        IIdempotencyStore idempotencyStore,
        ILogger<PaymentWorker> logger)
    {
        _kafkaOptions = kafkaOptions;
        _publisher = publisher;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.Value.BootstrapServers,
            GroupId = _kafkaOptions.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _kafkaOptions.Value.EnableAutoCommit,
            EnablePartitionEof = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_kafkaOptions.Value.Topic);

        _logger.LogInformation(
            "Payment worker started. Listening Kafka topic {Topic} with group {GroupId}",
            _kafkaOptions.Value.Topic,
            _kafkaOptions.Value.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    await ProcessMessageAsync(consumeResult, consumer, stoppingToken).ConfigureAwait(false);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while processing Kafka message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<Ignore, string> consumeResult,
        IConsumer<Ignore, string> consumer,
        CancellationToken stoppingToken)
    {
        CorrelationHeaderExtractor.ApplyFromKafka(consumeResult.Message.Headers);

        using (_logger.BeginScope(new Dictionary<string, object> { [CorrelationIdHeader.Name] = CorrelationContext.GetOrCreate() }))
        {
            await ProcessOrderAsync(consumeResult, consumer, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessOrderAsync(
        ConsumeResult<Ignore, string> consumeResult,
        IConsumer<Ignore, string> consumer,
        CancellationToken stoppingToken)
    {
        var order = JsonSerializer.Deserialize<OrderCreated>(consumeResult.Message.Value);

        if (order == null)
        {
            _logger.LogWarning("Received null order message");
            CommitOffset(consumer, consumeResult);
            return;
        }

        if (!_idempotencyStore.TryMarkProcessed(order.OrderId))
        {
            _logger.LogWarning("Duplicate order {OrderId} skipped", order.OrderId);
            CommitOffset(consumer, consumeResult);
            return;
        }

        _logger.LogInformation("Processing payment for order {OrderId}", order.OrderId);

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

        await Task.CompletedTask.ConfigureAwait(false);

        CommitOffset(consumer, consumeResult);
    }

    private static void CommitOffset(IConsumer<Ignore, string> consumer, ConsumeResult<Ignore, string> consumeResult)
    {
        try
        {
            consumer.Commit(consumeResult);
        }
        catch (KafkaException)
        {
            // Offset commit failures are typically transient; the message will be reprocessed.
            throw;
        }
    }

    private static bool ProcessPayment(OrderCreated order)
    {
        return order.Amount <= 10_000;
    }
}

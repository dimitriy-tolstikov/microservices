using System.Text.Json;
using Confluent.Kafka;
using Shared.Events;
using Shared.Messages;

namespace OrderService.Services;

public class OrderPublisher : IOrderPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OrderPublisher> _logger;

    public OrderPublisher(IConfiguration configuration, ILogger<OrderPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(OrderCreated order, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(order);
        var message = new Message<string, string>
        {
            Key = order.OrderId.ToString(),
            Value = json
        };

        var deliveryResult = await _producer.ProduceAsync(EventTypes.OrdersTopic, message, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} published to Kafka topic {Topic} partition {Partition} offset {Offset}",
            order.OrderId,
            deliveryResult.Topic,
            deliveryResult.Partition,
            deliveryResult.Offset);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}

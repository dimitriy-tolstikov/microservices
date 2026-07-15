using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Messages;
using Shared.Observability;
using Shared.Options;

namespace Shared.Messaging;

public sealed class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly KafkaOptions _options;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            MessageSendMaxRetries = _options.MessageSendMaxRetries,
            RetryBackoffMs = _options.RetryBackoffMs
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(OrderCreated order, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(order);
        var message = new Message<string, string>
        {
            Key = order.OrderId.ToString(),
            Value = json,
            Headers = new Headers
            {
                new Header(CorrelationIdHeader.Name, Encoding.UTF8.GetBytes(CorrelationContext.GetOrCreate()))
            }
        };

        var deliveryResult = await _producer.ProduceAsync(_options.Topic, message, cancellationToken);

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

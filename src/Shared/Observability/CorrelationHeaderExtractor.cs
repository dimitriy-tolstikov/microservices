using System.Text;
using Confluent.Kafka;
using RabbitMQ.Client.Events;

namespace Shared.Observability;

public static class CorrelationHeaderExtractor
{
    public static void ApplyFromKafka(Headers? headers)
    {
        if (headers == null)
        {
            CorrelationContext.GetOrCreate();
            return;
        }

        var header = headers.FirstOrDefault(h => h.Key == CorrelationIdHeader.Name);
        var correlationId = header != null && (header.GetValueBytes() ?? Array.Empty<byte>()) is { Length: > 0 } bytes
            ? Encoding.UTF8.GetString(bytes)
            : null;

        CorrelationContext.Current = !string.IsNullOrWhiteSpace(correlationId)
            ? correlationId
            : Guid.NewGuid().ToString("N");
    }

    public static void ApplyFromRabbitMq(BasicDeliverEventArgs eventArgs)
    {
        if (eventArgs.BasicProperties.Headers is not null &&
            eventArgs.BasicProperties.Headers.TryGetValue(CorrelationIdHeader.Name, out var value) &&
            value is byte[] bytes)
        {
            var correlationId = Encoding.UTF8.GetString(bytes);
            CorrelationContext.Current = !string.IsNullOrWhiteSpace(correlationId)
                ? correlationId
                : Guid.NewGuid().ToString("N");
        }
        else
        {
            CorrelationContext.GetOrCreate();
        }
    }
}

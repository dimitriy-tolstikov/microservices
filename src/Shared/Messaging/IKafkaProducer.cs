using Shared.Messages;

namespace Shared.Messaging;

public interface IKafkaProducer : IDisposable
{
    Task PublishAsync(OrderCreated order, CancellationToken cancellationToken = default);
}

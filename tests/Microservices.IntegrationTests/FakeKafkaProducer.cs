using Shared.Messages;
using Shared.Messaging;

namespace Microservices.IntegrationTests;

public sealed class FakeKafkaProducer : IKafkaProducer
{
    public List<OrderCreated> PublishedOrders { get; } = new();

    public Task PublishAsync(OrderCreated order, CancellationToken cancellationToken = default)
    {
        PublishedOrders.Add(order);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}

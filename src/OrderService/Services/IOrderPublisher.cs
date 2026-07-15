using Shared.Messages;

namespace OrderService.Services;

public interface IOrderPublisher
{
    Task PublishAsync(OrderCreated order, CancellationToken cancellationToken = default);
}

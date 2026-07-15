using Microsoft.Extensions.Options;
using Shared.Events;
using Shared.Messaging;
using Shared.Messages;
using Shared.Options;

namespace ShippingService.Workers;

public class ShippingWorker : RabbitMqConsumerBase<ShippingRequested>
{
    public ShippingWorker(
        IOptions<RabbitMqOptions> options,
        IMessageSerializer serializer,
        ILogger<ShippingWorker> logger)
        : base(options, serializer, logger)
    {
    }

    protected override string QueueName => EventTypes.ShippingQueue;

    protected override Task HandleMessageAsync(ShippingRequested shipping, CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Preparing shipment for order {OrderId}. Tracking: {TrackingNumber}",
            shipping.OrderId,
            shipping.TrackingNumber);

        // Имитация работы службы доставки
        Thread.Sleep(500);

        Logger.LogInformation(
            "Shipment for order {OrderId} dispatched successfully",
            shipping.OrderId);

        return Task.CompletedTask;
    }
}

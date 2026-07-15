using Shared.Messages;

namespace Shared.Messaging;

public interface IRabbitMqPublisher : IDisposable
{
    void PublishPaymentProcessed(PaymentProcessed message);
    void PublishShippingRequested(ShippingRequested message);
    void PublishNotificationRequested(NotificationRequested message);
}

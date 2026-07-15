using Shared.Messages;

namespace PaymentService.Services;

public interface IRabbitMqPublisher : IDisposable
{
    void PublishPaymentProcessed(PaymentProcessed message);
    void PublishShippingRequested(ShippingRequested message);
    void PublishNotificationRequested(NotificationRequested message);
}

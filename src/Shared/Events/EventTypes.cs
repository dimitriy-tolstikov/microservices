namespace Shared.Events;

public static class EventTypes
{
    // Kafka topics
    public const string OrdersTopic = "orders";

    // RabbitMQ exchanges and queues
    public const string PaymentExchange = "payments";
    public const string PaymentProcessedQueue = "payment.processed";

    public const string ShippingQueue = "shipping.requested";
    public const string NotificationQueue = "notification.requested";
}

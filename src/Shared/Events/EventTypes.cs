namespace Shared.Events;

public static class EventTypes
{
    // Kafka topics
    public const string OrdersTopic = "orders";

    // RabbitMQ exchanges and queues
    public const string PaymentExchange = "payments";
    public const string DeadLetterExchange = "payments.dlx";

    public const string PaymentProcessedQueue = "payment.processed";
    public const string ShippingQueue = "shipping.requested";
    public const string NotificationQueue = "notification.requested";

    public static string GetDeadLetterQueueName(string queueName) => $"{queueName}.dlq";
}

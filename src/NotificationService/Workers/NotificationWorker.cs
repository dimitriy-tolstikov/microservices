using Microsoft.Extensions.Options;
using Shared.Events;
using Shared.Messaging;
using Shared.Messages;
using Shared.Options;

namespace NotificationService.Workers;

public class NotificationWorker : RabbitMqConsumerBase<NotificationRequested>
{
    public NotificationWorker(
        IOptions<RabbitMqOptions> options,
        IMessageSerializer serializer,
        ILogger<NotificationWorker> logger)
        : base(options, serializer, logger)
    {
    }

    protected override string QueueName => EventTypes.NotificationQueue;

    protected override Task HandleMessageAsync(NotificationRequested notification, CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Sending email to {Email}: {Subject}",
            notification.CustomerEmail,
            notification.Subject);

        // Имитация отправки email
        Thread.Sleep(300);

        Logger.LogInformation(
            "Email sent for order {OrderId}",
            notification.OrderId);

        return Task.CompletedTask;
    }
}

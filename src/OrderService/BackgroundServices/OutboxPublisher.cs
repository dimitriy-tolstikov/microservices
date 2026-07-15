using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using Shared.Events;
using Shared.Messages;
using Shared.Messaging;

namespace OrderService.BackgroundServices;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(stoppingToken)
            .ConfigureAwait(false);

        foreach (var message in messages)
        {
            if (message.Type == typeof(OrderCreated).FullName)
            {
                var order = JsonSerializer.Deserialize<OrderCreated>(message.Payload);
                if (order != null)
                {
                    await producer.PublishAsync(order, stoppingToken).ConfigureAwait(false);
                }
            }

            message.ProcessedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

            _logger.LogInformation("Outbox message {MessageId} published", message.Id);
        }
    }
}

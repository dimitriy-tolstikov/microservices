using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using Shared.Observability;
using Shared.Options;

namespace Shared.Messaging;

public abstract class RabbitMqConsumerBase<TMessage> : BackgroundService, IDisposable
{
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly IMessageSerializer _serializer;
    private IConnection? _connection;
    private IModel? _channel;

    protected ILogger Logger { get; }

    protected RabbitMqConsumerBase(
        IOptions<RabbitMqOptions> options,
        IMessageSerializer serializer,
        ILogger logger)
    {
        _options = options;
        _serializer = serializer;
        Logger = logger;
    }

    protected abstract string QueueName { get; }

    protected abstract Task HandleMessageAsync(TMessage message, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        Initialize();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            Close();
        }
    }

    private void Initialize()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Value.HostName,
            Port = _options.Value.Port,
            UserName = _options.Value.UserName,
            Password = _options.Value.Password,
            VirtualHost = _options.Value.VirtualHost,
            DispatchConsumersAsync = _options.Value.DispatchConsumersAsync
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(EventTypes.PaymentExchange, ExchangeType.Direct, durable: true);
        _channel.ExchangeDeclare(EventTypes.DeadLetterExchange, ExchangeType.Direct, durable: true);

        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", EventTypes.DeadLetterExchange },
            { "x-dead-letter-routing-key", QueueName }
        };

        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments);
        _channel.QueueBind(QueueName, EventTypes.PaymentExchange, QueueName);

        var deadLetterQueueName = EventTypes.GetDeadLetterQueueName(QueueName);
        _channel.QueueDeclare(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(deadLetterQueueName, EventTypes.DeadLetterExchange, QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(QueueName, autoAck: false, consumer);

        Logger.LogInformation(
            "{ConsumerType} started. Listening queue {Queue}",
            GetType().Name,
            QueueName);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            CorrelationHeaderExtractor.ApplyFromRabbitMq(eventArgs);

            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var message = _serializer.Deserialize<TMessage>(json);

            if (message is null)
            {
                Logger.LogWarning("Received null {MessageType} message", typeof(TMessage).Name);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            await HandleMessageAsync(message, CancellationToken.None).ConfigureAwait(false);

            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing {MessageType} message", typeof(TMessage).Name);
            _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private void Close()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error closing RabbitMQ consumer connection");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }
    }

    public override void Dispose()
    {
        Close();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

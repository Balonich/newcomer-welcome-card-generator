using System.Text;
using System.Text.Json;
using Common.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Messaging.RabbitMQ
{
    public class RabbitMQConsumer<T> : IMessageConsumer<T>, IDisposable where T : class
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly string _routingKey;
        private readonly ILogger<RabbitMQConsumer<T>> _logger;
        private readonly RabbitMQSettings _settings;
        private AsyncEventingBasicConsumer _consumer;
        private bool _disposed;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public RabbitMQConsumer(
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQConsumer<T>> logger,
            string queueName,
            string exchangeName,
            string routingKey)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            _settings = settings.Value;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            try
            {
                // Without this locking mechanism, app could potentially end up with multiple connections to RabbitMQ and resource leaks,
                // which would be difficult to debug and could cause performance issues or even system failures.
                await _initializationLock.WaitAsync();

                if (_initialized) return; // Double-check after acquiring lock

                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    Port = _settings.Port
                };


                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                await _channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: _exchangeName,
                    routingKey: _routingKey);

                _logger.LogInformation("RabbitMQ consumer initialized for queue: {Queue}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ consumer");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }


        public async Task StartConsumingAsync(Func<T, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            await EnsureInitializedAsync();

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var data = JsonSerializer.Deserialize<T>(message);

                    if (data != null)
                    {
                        await handler(data);
                        await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: _consumer);

            _logger.LogInformation("Started consuming messages from queue: {Queue}", _queueName);
        }

        public async Task StopConsumingAsync()
        {
            await _channel?.BasicCancelAsync(_consumer?.ConsumerTags.FirstOrDefault());

            _logger.LogInformation("Stopped consuming messages from queue: {Queue}", _queueName);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _channel?.Dispose();
            _connection?.Dispose();

            _disposed = true;
        }
    }
}
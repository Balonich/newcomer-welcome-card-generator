using System.Text;
using System.Text.Json;
using Common.Interfaces.Messaging;
using Common.Messaging.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Common.BaseClasses.Messaging.RabbitMQ
{
    public class RabbitMQProducer<T> : IMessageProducer<T>, IDisposable where T : class
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly ILogger<RabbitMQProducer<T>> _logger;
        private readonly string _exchangeName;
        private readonly string _routingKey;
        private readonly RabbitMQSettings _settings;
        private bool _disposed;
        private bool _initialized = false;

        // SemaphoreSlim is used because:
        // 1. It supports async/await operations (standard C# lock doesn't)
        // 2. It allows asynchronous waiting without blocking threads
        // 3. It works well with the async connection setup for RabbitMQ
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public RabbitMQProducer(
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQProducer<T>> logger,
            string exchangeName,
            string routingKey)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

                _initialized = true;
                _logger.LogInformation($"RabbitMQ producer initialized for exchange: {_exchangeName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ producer");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task PublishAsync(T message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            await EnsureInitializedAsync();

            try
            {
                string jsonMessage = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                await _channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: _routingKey,
                    body: body);

                _logger.LogInformation($"Published message to {_exchangeName} with routing key {_routingKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _channel?.Dispose();
            _connection?.Dispose();
            _initializationLock?.Dispose();

            _disposed = true;
        }
    }
}
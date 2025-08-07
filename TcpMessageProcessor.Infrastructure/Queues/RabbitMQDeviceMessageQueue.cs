using RabbitMQ.Client;
using System.Text;
using TcpMessageProcessor.Core.Interfaces;

namespace TcpMessageProcessor.Infrastructure.Queues
{
    public class RabbitMQDeviceMessageQueue : IDeviceMessageQueue, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "device-messages";

        public RabbitMQDeviceMessageQueue(string connectionString = "amqp://admin:password123@localhost:5672/")
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(connectionString)
            };

            _connection = factory.CreateConnection("TcpMessageProcessor-DeviceMessages");
            _channel = _connection.CreateModel();

            // Declare queue with durability (survives RabbitMQ restarts)
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"🐰 RabbitMQ: Connected to queue '{_queueName}'");
        }

        public Task EnqueueAsync(string jsonMessage, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jsonMessage))
            {
                throw new ArgumentException("JSON message cannot be null or empty", nameof(jsonMessage));
            }

            try
            {
                var body = Encoding.UTF8.GetBytes(jsonMessage);
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Message survives RabbitMQ restarts

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _queueName,
                    basicProperties: properties,
                    body: body);

                Console.WriteLine($"🐰 RabbitMQ: Published device message to queue '{_queueName}' (size: {body.Length} bytes)");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🐰 RabbitMQ ERROR: Failed to publish message - {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }

    public class RabbitMQDeviceEventQueue : IDeviceEventQueue, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "device-events";

        public RabbitMQDeviceEventQueue(string connectionString = "amqp://admin:password123@localhost:5672/")
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(connectionString)
            };

            _connection = factory.CreateConnection("TcpMessageProcessor-DeviceEvents");
            _channel = _connection.CreateModel();

            // Declare queue with durability (survives RabbitMQ restarts)
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"🐰 RabbitMQ: Connected to queue '{_queueName}'");
        }

        public Task EnqueueAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            try
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Message survives RabbitMQ restarts

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _queueName,
                    basicProperties: properties,
                    body: payload);

                Console.WriteLine($"🐰 RabbitMQ: Published device event to queue '{_queueName}' (payload size: {payload.Length} bytes)");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🐰 RabbitMQ ERROR: Failed to publish event - {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
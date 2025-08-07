using System.Text.Json;
using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Infrastructure.Services
{
    public class MessageRouter : IMessageRouter
    {
        private readonly IDeviceMessageQueue _deviceMessageQueue;
        private readonly IDeviceEventQueue _deviceEventQueue;

        public MessageRouter(
            IDeviceMessageQueue deviceMessageQueue,
            IDeviceEventQueue deviceEventQueue)
        {
            _deviceMessageQueue = deviceMessageQueue ?? throw new ArgumentNullException(nameof(deviceMessageQueue));
            _deviceEventQueue = deviceEventQueue ?? throw new ArgumentNullException(nameof(deviceEventQueue));
        }

        public async Task RouteMessageAsync(DeviceMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (message.IsDeviceMessage())
                {
                    await ProcessDeviceMessageAsync(message, cancellationToken);
                }
                else if (message.IsDeviceEvent())
                {
                    await ProcessDeviceEventAsync(message, cancellationToken);
                }
                else
                {
                    // Unknown message type - could log to console or ignore for now
                    Console.WriteLine($"Unknown message type {message.MessageType} from device {message.GetDeviceIdHex()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to route message - DeviceId: {message.GetDeviceIdHex()}, Type: {message.MessageType}, Error: {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }

        private async Task ProcessDeviceMessageAsync(DeviceMessage message, CancellationToken cancellationToken)
        {
            // Convert payload to standardized JSON format
            var jsonMessage = ConvertToJson(message);
            await _deviceMessageQueue.EnqueueAsync(jsonMessage, cancellationToken);
        }

        private async Task ProcessDeviceEventAsync(DeviceMessage message, CancellationToken cancellationToken)
        {
            // Route payload directly to Device Event Queue
            await _deviceEventQueue.EnqueueAsync(message.Payload, cancellationToken);
        }

        private string ConvertToJson(DeviceMessage message)
        {
            // Create standardized JSON representation of Device Message
            var deviceMessageJson = new
            {
                DeviceId = message.GetDeviceIdHex(),
                MessageCounter = message.MessageCounter,
                MessageType = message.MessageType,
                Payload = Convert.ToBase64String(message.Payload),
                ReceivedAt = message.ReceivedAt,
                PayloadSize = message.Payload.Length
            };

            return JsonSerializer.Serialize(deviceMessageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false // Compact JSON for queue efficiency
            });
        }
    }
}
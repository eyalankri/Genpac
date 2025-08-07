using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Infrastructure.Services
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IDeduplicationService _deduplicationService;
        private readonly IMessageRouter _messageRouter;

        public MessageProcessor(
            IDeduplicationService deduplicationService,
            IMessageRouter messageRouter)
        {
            _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
            _messageRouter = messageRouter ?? throw new ArgumentNullException(nameof(messageRouter));
        }

        public async Task ProcessMessageAsync(DeviceMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                // Step 1: Check for duplicates
                var deduplicationResult = await _deduplicationService.CheckAndStoreAsync(message);

                if (deduplicationResult.IsDuplicate)
                {
                    Console.WriteLine($"Duplicate message discarded - DeviceId: {message.GetDeviceIdHex()}, Counter: {message.MessageCounter}");
                    return; // Discard duplicate
                }

                // Step 2: Route the unique message
                await _messageRouter.RouteMessageAsync(message, cancellationToken);

                Console.WriteLine($"Message processed successfully - DeviceId: {message.GetDeviceIdHex()}, Type: {message.MessageType}, Counter: {message.MessageCounter}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message - DeviceId: {message.GetDeviceIdHex()}, Error: {ex.Message}");
                throw; // Re-throw for higher-level error handling
            }
        }
    }
}
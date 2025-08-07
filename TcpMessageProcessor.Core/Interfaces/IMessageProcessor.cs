using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IMessageProcessor
    {
        /// <summary>
        /// Processes a message through deduplication, validation, and routing
        /// </summary>
        Task ProcessMessageAsync(DeviceMessage message, CancellationToken cancellationToken = default);
    }
}
using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IMessageRouter
    {
        /// <summary>
        /// Routes the message to appropriate queue based on message type
        /// </summary>
        Task RouteMessageAsync(DeviceMessage message, CancellationToken cancellationToken = default);
    }
}
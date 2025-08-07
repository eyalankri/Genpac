namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IDeviceMessageQueue
    {
        /// <summary>
        /// Enqueues a device message as JSON
        /// </summary>
        Task EnqueueAsync(string jsonMessage, CancellationToken cancellationToken = default);
    }
}
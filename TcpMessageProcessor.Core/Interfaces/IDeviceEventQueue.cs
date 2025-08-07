namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IDeviceEventQueue
    {
        /// <summary>
        /// Enqueues a device event payload directly
        /// </summary>
        Task EnqueueAsync(byte[] payload, CancellationToken cancellationToken = default);
    }
}
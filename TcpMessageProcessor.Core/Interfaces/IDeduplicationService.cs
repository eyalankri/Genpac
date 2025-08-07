using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IDeduplicationService
    {
        /// <summary>
        /// Checks if the message is a duplicate and stores it if unique
        /// </summary>
        Task<DeduplicationResult> CheckAndStoreAsync(DeviceMessage message);

        /// <summary>
        /// Clears old entries to prevent memory leaks
        /// </summary>
        Task ClearOldEntriesAsync(TimeSpan maxAge);
    }
}
using System.Collections.Concurrent;
using TcpMessageProcessor.Core.Interfaces;

namespace TcpMessageProcessor.Infrastructure.Queues
{
    public class InMemoryDeviceMessageQueue : IDeviceMessageQueue
    {
        private readonly ConcurrentQueue<string> _queue;

        public InMemoryDeviceMessageQueue()
        {
            _queue = new ConcurrentQueue<string>();
        }

        public Task EnqueueAsync(string jsonMessage, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jsonMessage))
            {
                throw new ArgumentException("JSON message cannot be null or empty", nameof(jsonMessage));
            }

            _queue.Enqueue(jsonMessage);

            return Task.CompletedTask;
        }

        // For testing/monitoring - not part of interface
        public bool TryDequeue(out string? message)
        {
            return _queue.TryDequeue(out message);
        }

        public int Count => _queue.Count;
    }

    public class InMemoryDeviceEventQueue : IDeviceEventQueue
    {
        private readonly ConcurrentQueue<byte[]> _queue;

        public InMemoryDeviceEventQueue()
        {
            _queue = new ConcurrentQueue<byte[]>();
        }

        public Task EnqueueAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            // Create a copy to avoid external modifications
            var payloadCopy = new byte[payload.Length];
            Array.Copy(payload, payloadCopy, payload.Length);

            _queue.Enqueue(payloadCopy);

            return Task.CompletedTask;
        }

        // For testing/monitoring - not part of interface
        public bool TryDequeue(out byte[]? payload)
        {
            return _queue.TryDequeue(out payload);
        }

        public int Count => _queue.Count;
    }
}
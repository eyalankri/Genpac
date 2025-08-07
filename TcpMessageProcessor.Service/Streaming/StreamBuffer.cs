using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Service.Streaming
{
    /// <summary>
    /// Manages a growing buffer for TCP stream data and extracts complete messages
    /// </summary>
    public class StreamBuffer
    {
        private byte[] _buffer;
        private int _dataLength;
        private readonly int _initialCapacity;
        private const int MaxBufferSize = 1024 * 1024; // 1MB limit to prevent memory exhaustion

        public StreamBuffer(int initialCapacity = 4096)
        {
            _initialCapacity = initialCapacity;
            _buffer = new byte[initialCapacity];
            _dataLength = 0;
        }

        /// <summary>
        /// Appends new data to the buffer
        /// </summary>
        public void AppendData(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > data.Length) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;

            // Check buffer size limit
            if (_dataLength + count > MaxBufferSize)
            {
                throw new InvalidOperationException($"Buffer would exceed maximum size of {MaxBufferSize} bytes");
            }

            // Ensure buffer has enough capacity
            EnsureCapacity(_dataLength + count);

            // Copy new data to buffer
            Array.Copy(data, offset, _buffer, _dataLength, count);
            _dataLength += count;
        }

        /// <summary>
        /// Tries to extract a complete message from the buffer
        /// </summary>
        public bool TryExtractMessage(IMessageParser parser, out DeviceMessage? message)
        {
            message = null;

            if (_dataLength < parser.MinimumMessageSize)
            {
                return false; // Not enough data for a complete message
            }

            // Try to parse a message from the current buffer
            var parseResult = parser.ParseMessage(new ReadOnlySpan<byte>(_buffer, 0, _dataLength));

            if (!parseResult.IsValid)
            {
                // If parsing failed due to insufficient data, keep waiting
                if (parseResult.ErrorMessage.Contains("Insufficient data"))
                {
                    return false;
                }

                // If it's a real parsing error, we need to handle it
                // For now, we'll try to find the next sync word by discarding one byte
                if (_dataLength > 0)
                {
                    RemoveBytes(1);
                }
                return false;
            }

            // Successfully parsed a message
            message = parseResult.Message;

            // Remove the consumed bytes from the buffer
            if (parseResult.BytesConsumed > 0)
            {
                RemoveBytes(parseResult.BytesConsumed);
            }

            return true;
        }

        /// <summary>
        /// Removes the specified number of bytes from the beginning of the buffer
        /// </summary>
        private void RemoveBytes(int count)
        {
            if (count <= 0 || count > _dataLength) return;

            // Shift remaining data to the beginning of the buffer
            if (count < _dataLength)
            {
                Array.Copy(_buffer, count, _buffer, 0, _dataLength - count);
            }

            _dataLength -= count;

            // If buffer is mostly empty, consider compacting it
            if (_dataLength < _initialCapacity / 4 && _buffer.Length > _initialCapacity)
            {
                CompactBuffer();
            }
        }

        /// <summary>
        /// Ensures the buffer has at least the specified capacity
        /// </summary>
        private void EnsureCapacity(int requiredCapacity)
        {
            if (_buffer.Length >= requiredCapacity) return;

            // Double the buffer size, but don't exceed the maximum
            int newCapacity = Math.Min(_buffer.Length * 2, MaxBufferSize);
            if (newCapacity < requiredCapacity)
            {
                newCapacity = Math.Min(requiredCapacity, MaxBufferSize);
            }

            var newBuffer = new byte[newCapacity];
            Array.Copy(_buffer, 0, newBuffer, 0, _dataLength);
            _buffer = newBuffer;
        }

        /// <summary>
        /// Reduces buffer size if it's much larger than needed
        /// </summary>
        private void CompactBuffer()
        {
            if (_buffer.Length <= _initialCapacity) return;

            var newBuffer = new byte[_initialCapacity];
            Array.Copy(_buffer, 0, newBuffer, 0, Math.Min(_dataLength, _initialCapacity));
            _buffer = newBuffer;
        }

        /// <summary>
        /// Clears all data from the buffer
        /// </summary>
        public void Clear()
        {
            _dataLength = 0;
            if (_buffer.Length > _initialCapacity)
            {
                _buffer = new byte[_initialCapacity];
            }
        }

        // Properties for monitoring
        public int DataLength => _dataLength;
        public int BufferCapacity => _buffer.Length;
    }
}
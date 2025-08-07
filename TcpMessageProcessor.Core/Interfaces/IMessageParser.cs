using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Core.Interfaces
{
    public interface IMessageParser
    {
        /// <summary>
        /// Attempts to parse a message from the buffer starting at the specified offset
        /// </summary>
        ParseResult ParseMessage(ReadOnlySpan<byte> buffer);

        /// <summary>
        /// Gets the minimum number of bytes needed to potentially contain a complete message
        /// </summary>
        int MinimumMessageSize { get; }
    }
}
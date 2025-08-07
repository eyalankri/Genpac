using System.Buffers.Binary;
using TcpMessageProcessor.Core.Constants;
using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Core.Models;

namespace TcpMessageProcessor.Infrastructure.Parsing
{
    public class BinaryMessageParser : IMessageParser
    {
        public int MinimumMessageSize => MessageConstants.MinimumMessageSize;

        public ParseResult ParseMessage(ReadOnlySpan<byte> buffer)
        {
            // Check if we have enough data for minimum message
            if (buffer.Length < MinimumMessageSize)
            {
                return ParseResult.InsufficientData();
            }

            // Find sync word
            int syncWordPosition = FindSyncWord(buffer);
            if (syncWordPosition == -1)
            {
                return ParseResult.Failure("Sync word not found");
            }

            // Adjust buffer to start at sync word
            buffer = buffer.Slice(syncWordPosition);

            // Check if we have enough data after sync word
            if (buffer.Length < MinimumMessageSize)
            {
                return ParseResult.InsufficientData();
            }

            try
            {
                return ParseMessageInternal(buffer, syncWordPosition);
            }
            catch (Exception ex)
            {
                return ParseResult.Failure($"Parsing error: {ex.Message}");
            }
        }

        private ParseResult ParseMessageInternal(ReadOnlySpan<byte> buffer, int syncWordOffset)
        {
            int position = 0;

            // 1. Verify Sync Word (2 bytes)
            ushort syncWord = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(position, 2));
            if (syncWord != MessageConstants.SyncWord)
            {
                return ParseResult.Failure($"Invalid sync word: 0x{syncWord:X4}");
            }
            position += MessageConstants.SyncWordSize;

            // 2. Read Device ID (4 bytes)
            byte[] deviceId = buffer.Slice(position, MessageConstants.DeviceIdSize).ToArray();
            position += MessageConstants.DeviceIdSize;

            // 3. Read Message Counter (2 bytes, Big Endian)
            ushort messageCounter = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(position, 2));
            position += MessageConstants.MessageCounterSize;

            // 4. Read Message Type (1 byte)
            byte messageType = buffer[position];
            position += MessageConstants.MessageTypeSize;

            // 5. Read Payload Length (2 bytes, Big Endian)
            ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(position, 2));
            position += MessageConstants.PayloadLengthSize;

            // 6. Validate total message size
            int totalMessageSize = MinimumMessageSize + payloadLength;
            if (buffer.Length < totalMessageSize)
            {
                return ParseResult.InsufficientData();
            }

            // 7. Read Payload
            byte[] payload = buffer.Slice(position, payloadLength).ToArray();
            position += payloadLength;

            // 8. Create DeviceMessage
            var message = new DeviceMessage
            {
                DeviceId = deviceId,
                MessageCounter = messageCounter,
                MessageType = messageType,
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            // Return success with total bytes consumed (including any skipped bytes before sync word)
            return ParseResult.Success(message, syncWordOffset + totalMessageSize);
        }

        private static int FindSyncWord(ReadOnlySpan<byte> buffer)
        {
            // Look for sync word pattern 0xAA55 in Big Endian
            byte syncByte1 = 0xAA;
            byte syncByte2 = 0x55;

            for (int i = 0; i <= buffer.Length - 2; i++)
            {
                if (buffer[i] == syncByte1 && buffer[i + 1] == syncByte2)
                {
                    return i;
                }
            }

            return -1; // Not found
        }
    }
}
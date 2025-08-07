namespace TcpMessageProcessor.Core.Constants
{
    public static class MessageConstants
    {
        public const ushort SyncWord = 0xAA55;
        public const int SyncWordSize = 2;
        public const int DeviceIdSize = 4;
        public const int MessageCounterSize = 2;
        public const int MessageTypeSize = 1;
        public const int PayloadLengthSize = 2;

        public const int MinimumMessageSize = SyncWordSize + DeviceIdSize + MessageCounterSize +
                                            MessageTypeSize + PayloadLengthSize; // 12 bytes minimum

        // Device Message Types (convert payload to JSON)
        public static readonly byte[] DeviceMessageTypes = { 2, 11, 13 };

        // Device Event Types (route payload directly)
        public static readonly byte[] DeviceEventTypes = { 1, 3, 12, 14 };
    }
}
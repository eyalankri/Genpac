using System;

namespace TcpMessageProcessor.Core.Models
{
    public class DeviceMessage
    {
        public byte[] DeviceId { get; set; } = Array.Empty<byte>();
        public ushort MessageCounter { get; set; }
        public byte MessageType { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public DateTime ReceivedAt { get; set; }

        public string GetDeviceIdHex()
        {
            return Convert.ToHexString(DeviceId);
        }

        public bool IsDeviceMessage()
        {
            return MessageType == 2 || MessageType == 11 || MessageType == 13;
        }

        public bool IsDeviceEvent()
        {
            return MessageType == 1 || MessageType == 3 || MessageType == 12 || MessageType == 14;
        }
    }
}
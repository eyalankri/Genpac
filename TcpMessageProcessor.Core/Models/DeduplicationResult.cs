namespace TcpMessageProcessor.Core.Models
{
    public class DeduplicationResult
    {
        public bool IsDuplicate { get; set; }
        public DeviceMessage Message { get; set; }

        public DeduplicationResult(bool isDuplicate, DeviceMessage message)
        {
            IsDuplicate = isDuplicate;
            Message = message;
        }

        public static DeduplicationResult Duplicate(DeviceMessage message)
        {
            return new DeduplicationResult(true, message);
        }

        public static DeduplicationResult Unique(DeviceMessage message)
        {
            return new DeduplicationResult(false, message);
        }
    }
}
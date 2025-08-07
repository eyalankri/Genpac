namespace TcpMessageProcessor.Core.Models
{
    public class ParseResult
    {
        public bool IsValid { get; set; }
        public DeviceMessage? Message { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int BytesConsumed { get; set; }

        public static ParseResult Success(DeviceMessage message, int bytesConsumed)
        {
            return new ParseResult
            {
                IsValid = true,
                Message = message,
                BytesConsumed = bytesConsumed
            };
        }

        public static ParseResult Failure(string errorMessage)
        {
            return new ParseResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                BytesConsumed = 0
            };
        }

        public static ParseResult InsufficientData()
        {
            return new ParseResult
            {
                IsValid = false,
                ErrorMessage = "Insufficient data",
                BytesConsumed = 0
            };
        }
    }
}
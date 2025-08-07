namespace TcpMessageProcessor.Service.Configuration
{
    public class TcpServerOptions
    {
        public const string SectionName = "TcpServer";

        /// <summary>
        /// TCP port to listen on
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Maximum number of concurrent client connections
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 1000;

        /// <summary>
        /// Buffer size for reading data from TCP streams
        /// </summary>
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// Timeout for client connections (in milliseconds)
        /// </summary>
        public int ClientTimeoutMs { get; set; } = 30000; // 30 seconds

        /// <summary>
        /// Maximum size for the stream buffer per connection
        /// </summary>
        public int MaxStreamBufferSize { get; set; } = 1024 * 1024; // 1MB
    }
}
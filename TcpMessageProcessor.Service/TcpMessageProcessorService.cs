using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Service.Configuration;
using TcpMessageProcessor.Service.Streaming;

namespace TcpMessageProcessor.Service
{
    public class TcpMessageProcessorService : BackgroundService
    {
        private readonly TcpServerOptions _options;
        private readonly IMessageParser _messageParser;
        private readonly IMessageProcessor _messageProcessor;
        private readonly SemaphoreSlim _connectionSemaphore;
        private TcpListener? _tcpListener;

        public TcpMessageProcessorService(
            IOptions<TcpServerOptions> options,
            IMessageParser messageParser,
            IMessageProcessor messageProcessor)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
            _connectionSemaphore = new SemaphoreSlim(_options.MaxConcurrentConnections, _options.MaxConcurrentConnections);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"Starting TCP Message Processor Service on port {_options.Port}");
            Console.WriteLine($"Max concurrent connections: {_options.MaxConcurrentConnections}");

            _tcpListener = new TcpListener(IPAddress.Any, _options.Port);
            _tcpListener.Start();

            Console.WriteLine($"TCP Server listening on port {_options.Port}");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Wait for a client connection
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();

                    Console.WriteLine($"Client connected from {tcpClient.Client.RemoteEndPoint}");

                    // Handle client in background (don't await to accept more connections)
                    _ = Task.Run(async () => await HandleClientAsync(tcpClient, stoppingToken), stoppingToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected when the service is stopping
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TCP server: {ex.Message}");
            }
            finally
            {
                _tcpListener?.Stop();
                Console.WriteLine("TCP Server stopped");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            // Acquire connection semaphore to limit concurrent connections
            await _connectionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Configure client settings
                client.ReceiveTimeout = _options.ClientTimeoutMs;
                client.SendTimeout = _options.ClientTimeoutMs;

                var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                Console.WriteLine($"Processing client: {clientEndpoint}");

                using (client)
                using (var stream = client.GetStream())
                {
                    await ProcessClientStreamAsync(stream, clientEndpoint, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                _connectionSemaphore.Release();
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
            }
        }

        private async Task ProcessClientStreamAsync(NetworkStream stream, string clientEndpoint, CancellationToken cancellationToken)
        {
            var buffer = new byte[_options.BufferSize];
            var streamBuffer = new StreamBuffer();

            try
            {
                while (!cancellationToken.IsCancellationRequested && stream.CanRead)
                {
                    // Read data from the client
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        // Client disconnected gracefully
                        break;
                    }

                    Console.WriteLine($"Received {bytesRead} bytes from {clientEndpoint}");

                    // Add received data to our stream buffer
                    streamBuffer.AppendData(buffer, 0, bytesRead);

                    // Process all complete messages in the buffer
                    await ProcessMessagesFromBuffer(streamBuffer, clientEndpoint, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing client stream {clientEndpoint}: {ex.Message}");
            }
        }

        private async Task ProcessMessagesFromBuffer(StreamBuffer streamBuffer, string clientEndpoint, CancellationToken cancellationToken)
        {
            // Keep extracting messages until no more complete messages are available
            while (streamBuffer.TryExtractMessage(_messageParser, out var message))
            {
                if (message != null)
                {
                    try
                    {
                        Console.WriteLine($"Extracted message from {clientEndpoint} - DeviceId: {message.GetDeviceIdHex()}, Type: {message.MessageType}, Counter: {message.MessageCounter}");

                        // Process the message through our pipeline
                        await _messageProcessor.ProcessMessageAsync(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message from {clientEndpoint}: {ex.Message}");
                        // Continue processing other messages even if one fails
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping TCP Message Processor Service...");

            _tcpListener?.Stop();

            // Wait for all connections to finish (with timeout)
            var timeout = TimeSpan.FromSeconds(10);
            var waitTask = Task.Run(async () =>
            {
                for (int i = 0; i < _options.MaxConcurrentConnections; i++)
                {
                    await _connectionSemaphore.WaitAsync(cancellationToken);
                    _connectionSemaphore.Release();
                }
            });

            await Task.WhenAny(waitTask, Task.Delay(timeout, cancellationToken));

            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _tcpListener?.Stop();
            _connectionSemaphore?.Dispose();
            base.Dispose();
        }
    }
}
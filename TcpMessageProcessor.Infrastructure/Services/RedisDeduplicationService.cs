using Microsoft.Extensions.Caching.Distributed;
using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Core.Models;
using System.Text;

namespace TcpMessageProcessor.Infrastructure.Services
{
    public class RedisDeduplicationService : IDeduplicationService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

        public RedisDeduplicationService(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<DeduplicationResult> CheckAndStoreAsync(DeviceMessage message)
        {
            var key = GenerateKey(message.DeviceId, message.MessageCounter);

            try
            {
                Console.WriteLine($"🔴 Redis: Checking key {key}");

                // Try to get existing value
                var existingValue = await _cache.GetAsync(key);

                if (existingValue != null)
                {
                    // Key exists - it's a duplicate
                    Console.WriteLine($"🔴 Redis: Found duplicate for key {key}");
                    return DeduplicationResult.Duplicate(message);
                }

                Console.WriteLine($"🔴 Redis: Key {key} not found, storing as new");

                // Key doesn't exist - store it with expiration
                var messageTimestamp = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O"));
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _defaultExpiration
                };

                await _cache.SetAsync(key, messageTimestamp, options);
                Console.WriteLine($"🔴 Redis: Successfully stored key {key} (expires in {_defaultExpiration.TotalHours} hours)");

                return DeduplicationResult.Unique(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Redis ERROR for key {key}: {ex.Message}");
                Console.WriteLine($"🔴 Redis: Falling back to assuming message is unique");
                // In case of Redis failure, assume it's unique to avoid blocking
                return DeduplicationResult.Unique(message);
            }
        }

        public async Task ClearOldEntriesAsync(TimeSpan maxAge)
        {
            // Redis handles expiration automatically, so this is a no-op
            // But we can implement manual cleanup if needed for specific patterns
            Console.WriteLine($"Redis: Automatic expiration handles cleanup (configured for {_defaultExpiration.TotalHours} hours)");
            await Task.CompletedTask;
        }

        private string GenerateKey(byte[] deviceId, ushort messageCounter)
        {
            // Create a Redis key with prefix for organization
            var deviceIdHex = Convert.ToHexString(deviceId);
            return $"tcp_msg_dedup:{deviceIdHex}:{messageCounter}";
        }

        // For monitoring purposes
        public async Task<bool> IsKeyStoredAsync(byte[] deviceId, ushort messageCounter)
        {
            var key = GenerateKey(deviceId, messageCounter);
            var value = await _cache.GetAsync(key);
            return value != null;
        }
    }
}
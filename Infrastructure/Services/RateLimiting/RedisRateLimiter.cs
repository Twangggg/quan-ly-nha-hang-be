namespace FoodHub.Infrastructure.Services.RateLimiting
{
    using FoodHub.Application.Interfaces;
    using StackExchange.Redis;

    public class RedisRateLimiter : IRateLimiter
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;

        public RedisRateLimiter(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        private static string FailKey(string key) => $"fail:{key}";
        private static string BlockKey(string key) => $"block:{key}";

        public async Task<bool> IsBlockedAsync(string key, CancellationToken ct)
        {
            if (!_redis.IsConnected)
                return false;

            try
            {
                return await _db.KeyExistsAsync(BlockKey(key));
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> RegisterFailAsync(
            string key,
            int limit,
            TimeSpan window,
            TimeSpan blockFor,
            CancellationToken ct)
        {
            if (!_redis.IsConnected)
                return 0;

            try
            {
                var failKey = FailKey(key);

                // 1) tăng count (atomic)
                var count = (int)await _db.StringIncrementAsync(failKey);

                // 2) nếu là lần đầu, set TTL cho cửa sổ thời gian
                if (count == 1)
                {
                    await _db.KeyExpireAsync(failKey, window);
                }

                // 3) nếu vượt ngưỡng -> block
                if (count >= limit)
                {
                    await _db.StringSetAsync(BlockKey(key), "1", blockFor);
                }

                return count;
            }
            catch
            {
                // Nếu Redis có vấn đề, coi như không track được fail
                return 0;
            }
        }

        public async Task ResetAsync(string key, CancellationToken ct)
        {
            if (!_redis.IsConnected)
                return;

            try
            {
                await _db.KeyDeleteAsync(FailKey(key));
                await _db.KeyDeleteAsync(BlockKey(key));
            }
            catch
            {
                // Ignore
            }
        }
    }
}

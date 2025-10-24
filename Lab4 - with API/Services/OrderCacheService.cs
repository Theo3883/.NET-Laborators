using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Services;

public class OrderCacheService : IOrderCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrderCacheService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lock = new();

    public OrderCacheService(IMemoryCache cache, ILogger<OrderCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void CacheOrder(string key, object value)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(key, value, cacheEntryOptions);

        lock (_lock)
        {
            _cacheKeys.Add(key);
        }

        _logger.LogDebug("Cached order data with key: {CacheKey}", key);
    }

    public T? GetCachedOrder<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", key);
        return null;
    }

    public void InvalidateOrderCache(string key)
    {
        _cache.Remove(key);
        lock (_lock)
        {
            _cacheKeys.Remove(key);
        }
        _logger.LogInformation("Invalidated cache for key: {CacheKey}", key);
    }

    public void InvalidateAllOrderCaches()
    {
        lock (_lock)
        {
            foreach (var key in _cacheKeys.ToList())
            {
                _cache.Remove(key);
            }
            _cacheKeys.Clear();
        }
        _logger.LogInformation("Invalidated all order caches");
    }
}

using Lab3.Model;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Services;

/// <summary>
/// Order caching service with category-based cache key management.
/// Supports separate cache keys for different order categories for improved performance.
/// </summary>
public class OrderCacheService : IOrderCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrderCacheService> _logger;
    
    // Track all cache keys for global invalidation
    private readonly HashSet<string> _cacheKeys = new();
    
    // Track cache keys by category for category-specific invalidation
    private readonly Dictionary<OrderCategory, HashSet<string>> _categoryCacheKeys = new();
    
    private readonly object _lock = new();

    public OrderCacheService(IMemoryCache cache, ILogger<OrderCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Initialize category cache key tracking
        foreach (OrderCategory category in Enum.GetValues<OrderCategory>())
        {
            _categoryCacheKeys[category] = new HashSet<string>();
        }
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
            
            // Clear all category-specific caches
            foreach (var category in _categoryCacheKeys.Keys)
            {
                _categoryCacheKeys[category].Clear();
            }
        }
        _logger.LogInformation("Invalidated all order caches across all categories");
    }

    /// <summary>
    /// Invalidate all cache entries for a specific order category.
    /// This allows selective cache invalidation when only one category is affected.
    /// </summary>
    public void InvalidateCategoryCache(OrderCategory category)
    {
        lock (_lock)
        {
            if (_categoryCacheKeys.TryGetValue(category, out var categoryKeys))
            {
                foreach (var key in categoryKeys.ToList())
                {
                    _cache.Remove(key);
                    _cacheKeys.Remove(key);
                }
                categoryKeys.Clear();
            }
        }
        _logger.LogInformation("Invalidated all caches for category: {Category} ({CategoryCount} keys removed)", 
            category, _categoryCacheKeys[category].Count);
    }

    /// <summary>
    /// Get the cache key for all orders of a specific category.
    /// Different cache keys for Fiction, Technical, etc.
    /// </summary>
    public string GetCategoryAllOrdersKey(OrderCategory category)
    {
        return $"orders_category_{category}_all";
    }

    /// <summary>
    /// Get the cache key for paginated orders of a specific category.
    /// </summary>
    public string GetCategoryPaginatedKey(OrderCategory category, int page, int pageSize)
    {
        return $"orders_category_{category}_page_{page}_size_{pageSize}";
    }

    /// <summary>
    /// Cache an order value and track it by category if applicable.
    /// </summary>
    public void CacheOrderByCategory(string key, object value, OrderCategory? category)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(key, value, cacheEntryOptions);

        lock (_lock)
        {
            _cacheKeys.Add(key);
            
            // Track by category if provided
            if (category.HasValue && _categoryCacheKeys.ContainsKey(category.Value))
            {
                _categoryCacheKeys[category.Value].Add(key);
            }
        }

        _logger.LogDebug("Cached order data with key: {CacheKey}, Category: {Category}", 
            key, category?.ToString() ?? "None");
    }
}

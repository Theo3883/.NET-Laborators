using Lab3.Model;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Services;

/// <summary>
/// Service for managing category-based book caching with smart invalidation
/// </summary>
public interface IBookCacheService
{
    /// <summary>
    /// Invalidates all cache entries for a specific book category
    /// </summary>
    void InvalidateCategoryCache(BookCategory category);
    
    /// <summary>
    /// Invalidates all book pagination cache entries
    /// </summary>
    void InvalidateAllBookCaches();
    
    /// <summary>
    /// Registers a cache key for tracking
    /// </summary>
    void RegisterCacheKey(string key);
    
    /// <summary>
    /// Records a cache hit for statistics
    /// </summary>
    void RecordCacheHit();
    
    /// <summary>
    /// Records a cache miss for statistics
    /// </summary>
    void RecordCacheMiss();
    
    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    BookCacheStats GetCacheStats();
}

public class BookCacheService : IBookCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<BookCacheService> _logger;
    private readonly HashSet<string> _activeCacheKeys = new();
    private readonly object _lock = new();
    
    // Cache statistics
    private int _cacheHits = 0;
    private int _cacheMisses = 0;
    private int _invalidations = 0;

    public BookCacheService(IMemoryCache cache, ILogger<BookCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void InvalidateCategoryCache(BookCategory category)
    {
        lock (_lock)
        {
            var keysToRemove = _activeCacheKeys
                .Where(k => k.Contains($"books_{category}_"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _activeCacheKeys.Remove(key);
                _invalidations++;
            }

            _logger.LogInformation(
                "Category-specific cache invalidation - Category: {Category}, KeysRemoved: {Count}", 
                category, keysToRemove.Count);
        }
    }

    public void InvalidateAllBookCaches()
    {
        lock (_lock)
        {
            var allBookKeys = _activeCacheKeys
                .Where(k => k.StartsWith("books_"))
                .ToList();

            foreach (var key in allBookKeys)
            {
                _cache.Remove(key);
                _activeCacheKeys.Remove(key);
                _invalidations++;
            }

            _logger.LogInformation(
                "All book caches invalidated - KeysRemoved: {Count}", 
                allBookKeys.Count);
        }
    }

    public void RegisterCacheKey(string key)
    {
        lock (_lock)
        {
            _activeCacheKeys.Add(key);
        }
    }

    public void RecordCacheHit()
    {
        Interlocked.Increment(ref _cacheHits);
    }

    public void RecordCacheMiss()
    {
        Interlocked.Increment(ref _cacheMisses);
    }

    public BookCacheStats GetCacheStats()
    {
        lock (_lock)
        {
            var total = _cacheHits + _cacheMisses;
            var hitRate = total > 0 ? (_cacheHits / (double)total) * 100 : 0;

            return new BookCacheStats(
                _cacheHits,
                _cacheMisses,
                hitRate,
                _invalidations,
                _activeCacheKeys.Count
            );
        }
    }
}

/// <summary>
/// Cache statistics for monitoring performance
/// </summary>
public record BookCacheStats(
    int CacheHits,
    int CacheMisses,
    double HitRatePercentage,
    int TotalInvalidations,
    int ActiveCacheKeys
);

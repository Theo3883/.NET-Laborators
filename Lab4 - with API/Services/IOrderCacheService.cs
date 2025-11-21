using Lab3.Model;

namespace Lab3.Services;

public interface IOrderCacheService
{
    void CacheOrder(string key, object value);
    T? GetCachedOrder<T>(string key) where T : class;
    void InvalidateOrderCache(string key);
    void InvalidateAllOrderCaches();
    
    // Category-based caching methods
    void InvalidateCategoryCache(OrderCategory category);
    string GetCategoryAllOrdersKey(OrderCategory category);
    string GetCategoryPaginatedKey(OrderCategory category, int page, int pageSize);
    void CacheOrderByCategory(string key, object value, OrderCategory? category);
}

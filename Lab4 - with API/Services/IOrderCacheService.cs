namespace Lab3.Services;

public interface IOrderCacheService
{
    void CacheOrder(string key, object value);
    T? GetCachedOrder<T>(string key) where T : class;
    void InvalidateOrderCache(string key);
    void InvalidateAllOrderCaches();
}

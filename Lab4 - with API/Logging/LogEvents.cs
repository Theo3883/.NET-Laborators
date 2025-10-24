namespace Lab3.Logging;


///  event IDs 
public static class LogEvents
{
    // Order Creation Events (2000-2099)
    public const int OrderCreationStarted = 2001;
    public const int OrderValidationFailed = 2002;
    public const int OrderCreationCompleted = 2003;
    public const int DatabaseOperationStarted = 2004;
    public const int DatabaseOperationCompleted = 2005;
    public const int CacheOperationPerformed = 2006;
    public const int ISBNValidationPerformed = 2007;
    public const int StockValidationPerformed = 2008;

    // Order Retrieval Events (2100-2199)
    public const int OrderRetrievalStarted = 2101;
    public const int OrderRetrievalCompleted = 2102;
    public const int OrderNotFound = 2103;

    // Order Update Events (2200-2299)
    public const int OrderUpdateStarted = 2201;
    public const int OrderUpdateCompleted = 2202;
    public const int OrderUpdateFailed = 2203;

    // Order Delete Events (2300-2399)
    public const int OrderDeleteStarted = 2301;
    public const int OrderDeleteCompleted = 2302;
    public const int OrderDeleteFailed = 2303;

    // Cache Events (2400-2499)
    public const int CacheHit = 2401;
    public const int CacheMiss = 2402;
    public const int CacheInvalidation = 2403;

    // Validation Events (2500-2599)
    public const int ValidationStarted = 2501;
    public const int ValidationCompleted = 2502;
    public const int ValidationFailed = 2503;
}

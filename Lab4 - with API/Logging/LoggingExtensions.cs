using Lab3.Model;

namespace Lab3.Logging;

/// Extension methods for structured logging in the Order management system
public static class LoggingExtensions
{

    public static void LogOrderCreationMetrics(this ILogger logger, OrderCreationMetrics metrics)
    {
        var eventId = new EventId(
            metrics.Success ? LogEvents.OrderCreationCompleted : LogEvents.OrderValidationFailed,
            metrics.Success ? "OrderCreationCompleted" : "OrderCreationFailed"
        );

        var logLevel = metrics.Success ? LogLevel.Information : LogLevel.Warning;

        logger.Log(
            logLevel,
            eventId,
            "Order creation operation - " +
            "OperationId: {OperationId}, " +
            "Title: {Title}, " +
            "ISBN: {ISBN}, " +
            "Category: {Category}, " +
            "ValidationDuration: {ValidationDuration}ms, " +
            "DatabaseSaveDuration: {DatabaseSaveDuration}ms, " +
            "TotalDuration: {TotalDuration}ms, " +
            "Success: {Success}" +
            (metrics.ErrorReason != null ? ", ErrorReason: {ErrorReason}" : ""),
            metrics.OperationId,
            metrics.OrderTitle,
            metrics.ISBN,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason
        );
    }

    /// Logs the start of an order creation operation
    public static void LogOrderCreationStarted(
        this ILogger logger,
        string operationId,
        string title,
        string author,
        string isbn,
        OrderCategory category)
    {
        logger.LogInformation(
            new EventId(LogEvents.OrderCreationStarted, nameof(LogEvents.OrderCreationStarted)),
            "Order creation operation started - " +
            "OperationId: {OperationId}, " +
            "Title: {Title}, " +
            "Author: {Author}, " +
            "ISBN: {ISBN}, " +
            "Category: {Category}",
            operationId,
            title,
            author,
            isbn,
            category
        );
    }
    
    /// Logs database operation start
    public static void LogDatabaseOperationStarted(
        this ILogger logger,
        string operationId,
        string operationType)
    {
        logger.LogDebug(
            new EventId(LogEvents.DatabaseOperationStarted, nameof(LogEvents.DatabaseOperationStarted)),
            "Database operation started - OperationId: {OperationId}, Type: {OperationType}",
            operationId,
            operationType
        );
    }
    
    /// Logs database operation completion
    public static void LogDatabaseOperationCompleted(
        this ILogger logger,
        string operationId,
        string operationType,
        string? entityId,
        long durationMs)
    {
        logger.LogInformation(
            new EventId(LogEvents.DatabaseOperationCompleted, nameof(LogEvents.DatabaseOperationCompleted)),
            "Database operation completed - " +
            "OperationId: {OperationId}, " +
            "Type: {OperationType}, " +
            "EntityId: {EntityId}, " +
            "Duration: {Duration}ms",
            operationId,
            operationType,
            entityId,
            durationMs
        );
    }
    
    /// Logs cache operation
    public static void LogCacheOperation(
        this ILogger logger,
        string operationId,
        string operation,
        string? cacheKey = null)
    {
        logger.LogDebug(
            new EventId(LogEvents.CacheOperationPerformed, nameof(LogEvents.CacheOperationPerformed)),
            "Cache operation performed - " +
            "OperationId: {OperationId}, " +
            "Operation: {Operation}" +
            (cacheKey != null ? ", CacheKey: {CacheKey}" : ""),
            operationId,
            operation,
            cacheKey
        );
    }
    
    /// Logs ISBN validation
    public static void LogISBNValidation(
        this ILogger logger,
        string operationId,
        string isbn,
        bool isUnique,
        long durationMs)
    {
        logger.LogDebug(
            new EventId(LogEvents.ISBNValidationPerformed, nameof(LogEvents.ISBNValidationPerformed)),
            "ISBN validation performed - " +
            "OperationId: {OperationId}, " +
            "ISBN: {ISBN}, " +
            "IsUnique: {IsUnique}, " +
            "Duration: {Duration}ms",
            operationId,
            isbn,
            isUnique,
            durationMs
        );
    }

    /// Logs stock validation
    public static void LogStockValidation(
        this ILogger logger,
        string operationId,
        int stockQuantity,
        bool isValid)
    {
        logger.LogDebug(
            new EventId(LogEvents.StockValidationPerformed, nameof(LogEvents.StockValidationPerformed)),
            "Stock validation performed - " +
            "OperationId: {OperationId}, " +
            "StockQuantity: {StockQuantity}, " +
            "IsValid: {IsValid}",
            operationId,
            stockQuantity,
            isValid
        );
    }
}

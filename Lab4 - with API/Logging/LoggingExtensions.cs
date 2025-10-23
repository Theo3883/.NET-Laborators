namespace Lab3.Logging;

public static class LoggingExtensions
{
    public static void LogBookCreationMetrics(
        this ILogger logger,
        BookCreationMetrics metrics)
    {
        if (metrics.Success)
        {
            logger.LogInformation(
                LogEvents.BookCreationCompleted,
                "Book creation completed successfully - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Category: {Category}, " +
                "ValidationDuration: {ValidationDurationMs}ms, DatabaseSaveDuration: {DatabaseSaveDurationMs}ms, " +
                "TotalDuration: {TotalDurationMs}ms, Success: {Success}",
                metrics.OperationId,
                metrics.BookTitle,
                metrics.ISBN,
                metrics.Category,
                metrics.ValidationDuration.TotalMilliseconds,
                metrics.DatabaseSaveDuration.TotalMilliseconds,
                metrics.TotalDuration.TotalMilliseconds,
                metrics.Success);
        }
        else
        {
            logger.LogError(
                LogEvents.BookCreationCompleted,
                "Book creation failed - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Category: {Category}, " +
                "ValidationDuration: {ValidationDurationMs}ms, DatabaseSaveDuration: {DatabaseSaveDurationMs}ms, " +
                "TotalDuration: {TotalDurationMs}ms, Success: {Success}, ErrorReason: {ErrorReason}",
                metrics.OperationId,
                metrics.BookTitle,
                metrics.ISBN,
                metrics.Category,
                metrics.ValidationDuration.TotalMilliseconds,
                metrics.DatabaseSaveDuration.TotalMilliseconds,
                metrics.TotalDuration.TotalMilliseconds,
                metrics.Success,
                metrics.ErrorReason);
        }
    }
}

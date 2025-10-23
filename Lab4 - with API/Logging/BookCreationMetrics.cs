using Lab3.Model;

namespace Lab3.Logging;

public record BookCreationMetrics(
    string OperationId,
    string BookTitle,
    string ISBN,
    BookCategory Category,
    TimeSpan ValidationDuration,
    TimeSpan DatabaseSaveDuration,
    TimeSpan TotalDuration,
    bool Success,
    string? ErrorReason = null
);

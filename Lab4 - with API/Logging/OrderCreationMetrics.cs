using Lab3.Model;

namespace Lab3.Logging;


public record OrderCreationMetrics
{
    /// Unique identifier for this operation (for tracing and correlation)
    public required string OperationId { get; init; }
    
    public required string OrderTitle { get; init; }
    
    public required string ISBN { get; init; }

    public required OrderCategory Category { get; init; }


    public required TimeSpan ValidationDuration { get; init; }
    
    public required TimeSpan DatabaseSaveDuration { get; init; }

    public required TimeSpan TotalDuration { get; init; }

    public required bool Success { get; init; }

    /// Error reason if the operation failed (null if successful)
    public string? ErrorReason { get; init; }
}

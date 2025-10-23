using Lab3.Model;

namespace Lab3.DTO;

/// metrics dashboard
public record BookMetricsDto(
    // Overall Statistics
    int TotalBooks,
    int TotalAvailableBooks,
    int TotalOutOfStockBooks,
    decimal TotalInventoryValue,
    
    // Category Breakdown
    Dictionary<string, CategoryMetrics> BooksByCategory,
    
    // Time-based Metrics
    int BooksCreatedToday,
    int BooksCreatedThisWeek,
    int BooksCreatedThisMonth,
    
    // Stock Metrics
    int LowStockBooks,
    int HighValueBooks,
    
    // Top Books
    BookProfileDto? MostExpensiveBook,
    BookProfileDto? NewestBook,
    BookProfileDto? OldestBook,
    List<BookProfileDto> TopStockBooks,
    
    // Performance Metrics
    decimal AverageBookPrice,
    decimal MedianBookPrice,
    DateTime? LastBookCreated,
    DateTime MetricsGeneratedAt
);

/// Metrics for a specific book category
public record CategoryMetrics(
    int TotalBooks,
    int AvailableBooks,
    int OutOfStockBooks,
    decimal AveragePrice,
    decimal TotalValue,
    decimal LowestPrice,
    decimal HighestPrice,
    int TotalStock
);

/// Real-time performance snapshot
public record BookPerformanceMetrics(
    int TotalBooksInSystem,
    double InventoryTurnoverRate,
    Dictionary<string, int> CategoryDistribution,
    int BooksAddedLast24Hours,
    int BooksAddedLast7Days,
    double AveragePriceChange,
    DateTime SnapshotTime
);

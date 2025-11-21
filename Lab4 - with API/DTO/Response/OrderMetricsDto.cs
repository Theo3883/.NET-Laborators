namespace Lab3.DTO.Response;

public class OrderMetricsDto
{
    public OrderCreationMetrics OrderCreation { get; set; } = null!;
    public InventoryMetrics Inventory { get; set; } = null!;
    public CategoryMetrics CategoryBreakdown { get; set; } = null!;
    public PerformanceMetrics Performance { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedAtUtc { get; set; } = null!;
}

public class OrderCreationMetrics
{
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersThisWeek { get; set; }
    public int OrdersThisMonth { get; set; }
    public decimal TotalRevenue { get; set; }
    public string FormattedTotalRevenue { get; set; } = null!;
    public decimal AverageOrderValue { get; set; }
    public string FormattedAverageOrderValue { get; set; } = null!;
    public OrderTimeSeriesDto RecentActivity { get; set; } = null!;
}

public class OrderTimeSeriesDto
{
    public int Last24Hours { get; set; }
    public int Last7Days { get; set; }
    public int Last30Days { get; set; }
    public List<DailyOrderCountDto> DailyBreakdown { get; set; } = new();
}

public class DailyOrderCountDto
{
    public string Date { get; set; } = null!;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    public string FormattedRevenue { get; set; } = null!;
}

public class InventoryMetrics
{
    public int TotalStock { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public string FormattedTotalInventoryValue { get; set; } = null!;
    public List<TopStockItemDto> TopStockItems { get; set; } = new();
    public List<LowStockItemDto> LowStockAlerts { get; set; } = new();
}

public class TopStockItemDto
{
    public string OrderId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int StockQuantity { get; set; }
    public decimal Value { get; set; }
    public string FormattedValue { get; set; } = null!;
}

public class LowStockItemDto
{
    public string OrderId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int StockQuantity { get; set; }
    public string Category { get; set; } = null!;
}

public class CategoryMetrics
{
    public List<CategoryBreakdownDto> Categories { get; set; } = new();
    public string MostPopularCategory { get; set; } = null!;
    public string HighestRevenueCategory { get; set; } = null!;
}

public class CategoryBreakdownDto
{
    public string CategoryName { get; set; } = null!;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string FormattedTotalRevenue { get; set; } = null!;
    public decimal AveragePrice { get; set; }
    public string FormattedAveragePrice { get; set; } = null!;
    public int TotalStock { get; set; }
    public double PercentageOfTotal { get; set; }
}

public class PerformanceMetrics
{
    public CachePerformanceDto CachePerformance { get; set; } = null!;
    public ValidationPerformanceDto ValidationPerformance { get; set; } = null!;
    public DatabasePerformanceDto DatabasePerformance { get; set; } = null!;
}

public class CachePerformanceDto
{
    public int TotalCacheKeys { get; set; }
    public Dictionary<string, int> KeysByCategory { get; set; } = new();
    public string MostCachedCategory { get; set; } = null!;
}

public class ValidationPerformanceDto
{
    public double AverageValidationTimeMs { get; set; }
    public int TotalValidations { get; set; }
    public string FastestValidationMs { get; set; } = null!;
    public string SlowestValidationMs { get; set; } = null!;
}

public class DatabasePerformanceDto
{
    public double AverageSaveTimeMs { get; set; }
    public int TotalDatabaseOperations { get; set; }
    public string FastestOperationMs { get; set; } = null!;
    public string SlowestOperationMs { get; set; } = null!;
}

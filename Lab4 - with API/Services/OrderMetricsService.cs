using System.Collections.Concurrent;
using Lab3.DTO.Response;
using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Services;

public class OrderMetricsService : IOrderMetricsService
{
    private readonly BookContext _context;
    private readonly IOrderCacheService _cacheService;
    private readonly PerformanceMetricsCollector _metricsCollector;
    private const int LowStockThreshold = 20;

    public OrderMetricsService(
        BookContext context, 
        IOrderCacheService cacheService,
        PerformanceMetricsCollector metricsCollector)
    {
        _context = context;
        _cacheService = cacheService;
        _metricsCollector = metricsCollector;
    }

    public void RecordValidationTime(double milliseconds)
    {
        _metricsCollector.RecordValidationTime(milliseconds);
    }

    public void RecordDatabaseOperationTime(double milliseconds)
    {
        _metricsCollector.RecordDatabaseOperationTime(milliseconds);
    }

    public async Task<OrderMetricsDto> GetOrderMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);

        // Fetch all orders for aggregation
        var allOrders = await _context.Orders
            .AsNoTracking()
            .ToListAsync();

        var totalOrders = allOrders.Count;
        var ordersToday = allOrders.Count(o => o.CreatedAt.Date == today);
        var ordersThisWeek = allOrders.Count(o => o.CreatedAt >= weekAgo);
        var ordersThisMonth = allOrders.Count(o => o.CreatedAt >= monthAgo);

        var totalRevenue = allOrders.Sum(o => o.Price * o.StockQuantity);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Daily breakdown for last 7 days
        var dailyBreakdown = allOrders
            .Where(o => o.CreatedAt >= weekAgo)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailyOrderCountDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Revenue = g.Sum(o => o.Price * o.StockQuantity),
                FormattedRevenue = $"${g.Sum(o => o.Price * o.StockQuantity):N2}"
            })
            .OrderByDescending(d => d.Date)
            .ToList();

        // Inventory metrics
        var totalStock = allOrders.Sum(o => o.StockQuantity);
        var lowStockItems = allOrders.Count(o => o.StockQuantity > 0 && o.StockQuantity < LowStockThreshold);
        var outOfStockItems = allOrders.Count(o => o.StockQuantity == 0);
        var totalInventoryValue = allOrders.Sum(o => o.Price * o.StockQuantity);

        var topStockItems = allOrders
            .OrderByDescending(o => o.StockQuantity)
            .Take(5)
            .Select(o => new TopStockItemDto
            {
                OrderId = o.Id.ToString(),
                Title = o.Title,
                StockQuantity = o.StockQuantity,
                Value = o.Price * o.StockQuantity,
                FormattedValue = $"${o.Price * o.StockQuantity:N2}"
            })
            .ToList();

        var lowStockAlerts = allOrders
            .Where(o => o.StockQuantity > 0 && o.StockQuantity < LowStockThreshold)
            .OrderBy(o => o.StockQuantity)
            .Take(10)
            .Select(o => new LowStockItemDto
            {
                OrderId = o.Id.ToString(),
                Title = o.Title,
                StockQuantity = o.StockQuantity,
                Category = GetCategoryDisplayName(o.Category)
            })
            .ToList();

        // Category breakdown
        var categoryBreakdown = allOrders
            .GroupBy(o => o.Category)
            .Select(g => new CategoryBreakdownDto
            {
                CategoryName = GetCategoryDisplayName(g.Key),
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.Price * o.StockQuantity),
                FormattedTotalRevenue = $"${g.Sum(o => o.Price * o.StockQuantity):N2}",
                AveragePrice = g.Average(o => o.Price),
                FormattedAveragePrice = $"${g.Average(o => o.Price):N2}",
                TotalStock = g.Sum(o => o.StockQuantity),
                PercentageOfTotal = totalOrders > 0 ? Math.Round((double)g.Count() / totalOrders * 100, 2) : 0
            })
            .OrderByDescending(c => c.OrderCount)
            .ToList();

        var mostPopularCategory = categoryBreakdown.FirstOrDefault()?.CategoryName ?? "N/A";
        var highestRevenueCategory = categoryBreakdown
            .OrderByDescending(c => c.TotalRevenue)
            .FirstOrDefault()?.CategoryName ?? "N/A";

        // Cache performance (category-based tracking)
        var keysByCategory = new Dictionary<string, int>();
        foreach (OrderCategory category in Enum.GetValues(typeof(OrderCategory)))
        {
            var categoryName = GetCategoryDisplayName(category);
            // Estimate: count orders in category as proxy for cache keys
            var count = allOrders.Count(o => o.Category == category);
            if (count > 0)
            {
                keysByCategory[categoryName] = count;
            }
        }

        var mostCachedCategory = keysByCategory
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault().Key ?? "N/A";

        // Validation and database performance from singleton collector
        var (validationTimesArray, databaseTimesArray) = _metricsCollector.GetMetrics();

        var avgValidationTime = validationTimesArray.Length > 0 ? validationTimesArray.Average() : 0;
        var avgDatabaseTime = databaseTimesArray.Length > 0 ? databaseTimesArray.Average() : 0;

        var fastestValidation = validationTimesArray.Length > 0 ? validationTimesArray.Min().ToString("F2") : "N/A";
        var slowestValidation = validationTimesArray.Length > 0 ? validationTimesArray.Max().ToString("F2") : "N/A";
        var fastestDbOp = databaseTimesArray.Length > 0 ? databaseTimesArray.Min().ToString("F2") : "N/A";
        var slowestDbOp = databaseTimesArray.Length > 0 ? databaseTimesArray.Max().ToString("F2") : "N/A";

        return new OrderMetricsDto
        {
            OrderCreation = new OrderCreationMetrics
            {
                TotalOrders = totalOrders,
                OrdersToday = ordersToday,
                OrdersThisWeek = ordersThisWeek,
                OrdersThisMonth = ordersThisMonth,
                TotalRevenue = totalRevenue,
                FormattedTotalRevenue = $"${totalRevenue:N2}",
                AverageOrderValue = averageOrderValue,
                FormattedAverageOrderValue = $"${averageOrderValue:N2}",
                RecentActivity = new OrderTimeSeriesDto
                {
                    Last24Hours = ordersToday,
                    Last7Days = ordersThisWeek,
                    Last30Days = ordersThisMonth,
                    DailyBreakdown = dailyBreakdown
                }
            },
            Inventory = new InventoryMetrics
            {
                TotalStock = totalStock,
                LowStockItems = lowStockItems,
                OutOfStockItems = outOfStockItems,
                TotalInventoryValue = totalInventoryValue,
                FormattedTotalInventoryValue = $"${totalInventoryValue:N2}",
                TopStockItems = topStockItems,
                LowStockAlerts = lowStockAlerts
            },
            CategoryBreakdown = new CategoryMetrics
            {
                Categories = categoryBreakdown,
                MostPopularCategory = mostPopularCategory,
                HighestRevenueCategory = highestRevenueCategory
            },
            Performance = new PerformanceMetrics
            {
                CachePerformance = new CachePerformanceDto
                {
                    TotalCacheKeys = keysByCategory.Values.Sum(),
                    KeysByCategory = keysByCategory,
                    MostCachedCategory = mostCachedCategory
                },
                ValidationPerformance = new ValidationPerformanceDto
                {
                    AverageValidationTimeMs = Math.Round(avgValidationTime, 2),
                    TotalValidations = validationTimesArray.Length,
                    FastestValidationMs = fastestValidation,
                    SlowestValidationMs = slowestValidation
                },
                DatabasePerformance = new DatabasePerformanceDto
                {
                    AverageSaveTimeMs = Math.Round(avgDatabaseTime, 2),
                    TotalDatabaseOperations = databaseTimesArray.Length,
                    FastestOperationMs = fastestDbOp,
                    SlowestOperationMs = slowestDbOp
                }
            },
            GeneratedAt = now,
            GeneratedAtUtc = now.ToString("o")
        };
    }

    private static string GetCategoryDisplayName(OrderCategory category)
    {
        return category switch
        {
            OrderCategory.Fiction => "Fiction & Literature",
            OrderCategory.NonFiction => "Non-Fiction",
            OrderCategory.Technical => "Technical & Professional",
            OrderCategory.Children => "Children's Orders",
            _ => category.ToString()
        };
    }
}

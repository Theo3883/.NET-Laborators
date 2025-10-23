using AutoMapper;
using Lab3.DTO;
using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;


/// Handler for retrieving comprehensive book metrics and analytics
public class GetBookMetricsHandler(
    BookContext context,
    IMapper mapper,
    ILogger<GetBookMetricsHandler> logger)
{
    public async Task<IResult> Handle()
    {
        logger.LogInformation("Generating book metrics dashboard");
        var startTime = DateTime.UtcNow;

        try
        {
            // Fetch all books in a single query for efficiency
            var allBooks = await context.Books
                .AsNoTracking()
                .ToListAsync();

            if (allBooks.Count == 0)
            {
                logger.LogWarning("No books found in database for metrics");
                return Results.Ok(CreateEmptyMetrics());
            }

            // Overall Statistics
            var totalBooks = allBooks.Count;
            var availableBooks = allBooks.Count(b => b.IsAvailable);
            var outOfStockBooks = totalBooks - availableBooks;
            var totalInventoryValue = allBooks.Sum(b => b.Price * b.StockQuantity);

            // Category Breakdown
            var categoryMetrics = allBooks
                .GroupBy(b => b.Category)
                .ToDictionary(
                    g => GetCategoryDisplayName(g.Key),
                    g => new CategoryMetrics(
                        TotalBooks: g.Count(),
                        AvailableBooks: g.Count(b => b.IsAvailable),
                        OutOfStockBooks: g.Count(b => !b.IsAvailable),
                        AveragePrice: g.Average(b => b.Price),
                        TotalValue: g.Sum(b => b.Price * b.StockQuantity),
                        LowestPrice: g.Min(b => b.Price),
                        HighestPrice: g.Max(b => b.Price),
                        TotalStock: g.Sum(b => b.StockQuantity)
                    )
                );

            // Time-based Metrics
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.AddDays(-7);
            var monthStart = now.AddDays(-30);

            var booksCreatedToday = allBooks.Count(b => b.CreatedAt >= todayStart);
            var booksCreatedThisWeek = allBooks.Count(b => b.CreatedAt >= weekStart);
            var booksCreatedThisMonth = allBooks.Count(b => b.CreatedAt >= monthStart);

            // Stock Metrics
            var lowStockBooks = allBooks.Count(b => b.StockQuantity > 0 && b.StockQuantity <= 5);
            var highValueBooks = allBooks.Count(b => b.Price > 100);

            // Top Books
            var mostExpensiveBook = allBooks.OrderByDescending(b => b.Price).FirstOrDefault();
            var newestBook = allBooks.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
            var oldestBook = allBooks.OrderBy(b => b.PublishedDate).FirstOrDefault();
            var topStockBooks = allBooks
                .OrderByDescending(b => b.StockQuantity)
                .Take(5)
                .ToList();

            // Map to DTOs
            var mostExpensiveDto = mostExpensiveBook != null ? mapper.Map<BookProfileDto>(mostExpensiveBook) : null;
            var newestDto = newestBook != null ? mapper.Map<BookProfileDto>(newestBook) : null;
            var oldestDto = oldestBook != null ? mapper.Map<BookProfileDto>(oldestBook) : null;
            var topStockDtos = mapper.Map<List<BookProfileDto>>(topStockBooks);

            // Price Statistics
            var averagePrice = allBooks.Average(b => b.Price);
            var sortedPrices = allBooks.Select(b => b.Price).OrderBy(p => p).ToList();
            var medianPrice = sortedPrices.Count % 2 == 0
                ? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2
                : sortedPrices[sortedPrices.Count / 2];

            var lastBookCreated = allBooks.Max(b => b.CreatedAt);

            var metrics = new BookMetricsDto(
                TotalBooks: totalBooks,
                TotalAvailableBooks: availableBooks,
                TotalOutOfStockBooks: outOfStockBooks,
                TotalInventoryValue: totalInventoryValue,
                BooksByCategory: categoryMetrics,
                BooksCreatedToday: booksCreatedToday,
                BooksCreatedThisWeek: booksCreatedThisWeek,
                BooksCreatedThisMonth: booksCreatedThisMonth,
                LowStockBooks: lowStockBooks,
                HighValueBooks: highValueBooks,
                MostExpensiveBook: mostExpensiveDto,
                NewestBook: newestDto,
                OldestBook: oldestDto,
                TopStockBooks: topStockDtos,
                AverageBookPrice: averagePrice,
                MedianBookPrice: medianPrice,
                LastBookCreated: lastBookCreated,
                MetricsGeneratedAt: DateTime.UtcNow
            );

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation(
                "Book metrics generated successfully - TotalBooks: {TotalBooks}, Duration: {DurationMs}ms",
                totalBooks, duration.TotalMilliseconds);

            return Results.Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating book metrics");
            throw;
        }
    }

    public async Task<IResult> HandlePerformanceMetrics()
    {
        logger.LogInformation("Generating real-time performance metrics");

        try
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);

            var totalBooks = await context.Books.CountAsync();
            
            var categoryDistribution = await context.Books
                .GroupBy(b => b.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    x => GetCategoryDisplayName(x.Category),
                    x => x.Count
                );

            var booksLast24Hours = await context.Books
                .CountAsync(b => b.CreatedAt >= last24Hours);

            var booksLast7Days = await context.Books
                .CountAsync(b => b.CreatedAt >= last7Days);

            // Calculate inventory turnover (books added vs total inventory)
            var turnoverRate = totalBooks > 0 ? (booksLast7Days / (double)totalBooks) * 100 : 0;

            var performanceMetrics = new BookPerformanceMetrics(
                TotalBooksInSystem: totalBooks,
                InventoryTurnoverRate: turnoverRate,
                CategoryDistribution: categoryDistribution,
                BooksAddedLast24Hours: booksLast24Hours,
                BooksAddedLast7Days: booksLast7Days,
                AveragePriceChange: 0.0, // Placeholder for future price tracking
                SnapshotTime: now
            );

            logger.LogInformation(
                "Performance metrics generated - TotalBooks: {TotalBooks}, Last24h: {Last24h}, Last7d: {Last7d}",
                totalBooks, booksLast24Hours, booksLast7Days);

            return Results.Ok(performanceMetrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating performance metrics");
            throw;
        }
    }

    private static BookMetricsDto CreateEmptyMetrics()
    {
        return new BookMetricsDto(
            TotalBooks: 0,
            TotalAvailableBooks: 0,
            TotalOutOfStockBooks: 0,
            TotalInventoryValue: 0,
            BooksByCategory: new Dictionary<string, CategoryMetrics>(),
            BooksCreatedToday: 0,
            BooksCreatedThisWeek: 0,
            BooksCreatedThisMonth: 0,
            LowStockBooks: 0,
            HighValueBooks: 0,
            MostExpensiveBook: null,
            NewestBook: null,
            OldestBook: null,
            TopStockBooks: new List<BookProfileDto>(),
            AverageBookPrice: 0,
            MedianBookPrice: 0,
            LastBookCreated: null,
            MetricsGeneratedAt: DateTime.UtcNow
        );
    }

    private static string GetCategoryDisplayName(BookCategory category)
    {
        return category switch
        {
            BookCategory.Fiction => "Fiction & Literature",
            BookCategory.NonFiction => "Non-Fiction",
            BookCategory.Technical => "Technical & Professional",
            BookCategory.Children => "Children's Books",
            _ => "Uncategorized"
        };
    }
}

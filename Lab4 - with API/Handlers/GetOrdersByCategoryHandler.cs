using AutoMapper;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;

/// <summary>
/// Handler for retrieving orders filtered by category with category-based caching.
/// Implements separate cache keys for different order categories for improved performance.
/// </summary>
public class GetOrdersByCategoryHandler
{
    private readonly BookContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrdersByCategoryHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public GetOrdersByCategoryHandler(
        BookContext context,
        IMapper mapper,
        ILogger<GetOrdersByCategoryHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<Ok<List<OrderProfileDto>>, ValidationProblem>> Handle(
        string categoryString, 
        HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        // Validate category
        if (!Enum.TryParse<OrderCategory>(categoryString, true, out var category))
        {
            _logger.LogWarning("Invalid category requested: {Category} - TraceId: {TraceId}", categoryString, traceId);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Category"] = new[] { "Category must be one of: Fiction, NonFiction, Technical, Children" }
            });
        }

        // Use category-specific cache key
        var cacheKey = _cacheService.GetCategoryAllOrdersKey(category);
        var cachedOrders = _cacheService.GetCachedOrder<List<OrderProfileDto>>(cacheKey);
        
        if (cachedOrders != null)
        {
            _logger.LogInformation(
                "Retrieved {Count} orders for category {Category} from cache - TraceId: {TraceId}", 
                cachedOrders.Count, category, traceId);
            return TypedResults.Ok(cachedOrders);
        }

        // Query database for specific category
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Category == category)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var orderDtos = _mapper.Map<List<OrderProfileDto>>(orders);

        // Cache with category-specific key
        _cacheService.CacheOrderByCategory(cacheKey, orderDtos, category);

        _logger.LogInformation(
            "Retrieved {Count} orders for category {Category} from database - TraceId: {TraceId}", 
            orderDtos.Count, category, traceId);
        
        return TypedResults.Ok(orderDtos);
    }
}

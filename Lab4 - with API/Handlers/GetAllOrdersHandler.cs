using AutoMapper;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;

public class GetAllOrdersHandler
{
    private readonly BookContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllOrdersHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public GetAllOrdersHandler(
        BookContext context,
        IMapper mapper,
        ILogger<GetAllOrdersHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Ok<List<OrderProfileDto>>> Handle(GetAllOrdersRequest request, HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        _logger.LogInformation("Getting all orders - TraceId: {TraceId}", traceId);

        // Check cache
        const string cacheKey = "all_orders";
        var cachedOrders = _cacheService.GetCachedOrder<List<OrderProfileDto>>(cacheKey);
        if (cachedOrders != null)
        {
            _logger.LogInformation("Retrieved {Count} orders from cache - TraceId: {TraceId}", cachedOrders.Count, traceId);
            return TypedResults.Ok(cachedOrders);
        }

        // Query database
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var orderDtos = _mapper.Map<List<OrderProfileDto>>(orders);

        // Cache the result
        _cacheService.CacheOrder(cacheKey, orderDtos);

        _logger.LogInformation("Retrieved {Count} orders from database", orderDtos.Count);
        return TypedResults.Ok(orderDtos);
    }
}

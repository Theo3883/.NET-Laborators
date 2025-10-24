using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;

public class GetOrdersWithPaginationHandler
{
    private readonly BookContext _context;
    private readonly IValidator<GetOrdersWithPaginationRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrdersWithPaginationHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public GetOrdersWithPaginationHandler(
        BookContext context,
        IValidator<GetOrdersWithPaginationRequest> validator,
        IMapper mapper,
        ILogger<GetOrdersWithPaginationHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<Ok<PagedResult<OrderProfileDto>>, ValidationProblem>> Handle(
        GetOrdersWithPaginationRequest request,
        HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        _logger.LogInformation("Getting orders with pagination - Page: {Page}, PageSize: {PageSize}, TraceId: {TraceId}", 
            request.Page, request.PageSize, traceId);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for pagination request - TraceId: {TraceId}", traceId);
            var extensions = new Dictionary<string, object?> { ["traceId"] = traceId };
            return TypedResults.ValidationProblem(validationResult.ToDictionary(), extensions: extensions);
        }

        // Check cache
        var cacheKey = $"orders_page_{request.Page}_size_{request.PageSize}";
        var cachedResult = _cacheService.GetCachedOrder<PagedResult<OrderProfileDto>>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Retrieved paginated orders from cache - TraceId: {TraceId}", traceId);
            return TypedResults.Ok(cachedResult);
        }

        // Query database
        var totalCount = await _context.Orders.CountAsync();
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var orderDtos = _mapper.Map<List<OrderProfileDto>>(orders);

        var result = new PagedResult<OrderProfileDto>
        {
            Items = orderDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };

        // Cache the result
        _cacheService.CacheOrder(cacheKey, result);

        _logger.LogInformation("Retrieved {Count} orders (Page {Page}/{TotalPages})", orderDtos.Count, request.Page, result.TotalPages);
        return TypedResults.Ok(result);
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

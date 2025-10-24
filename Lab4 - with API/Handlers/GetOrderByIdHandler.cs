using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Logging;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Lab3.Handlers;

public class GetOrderByIdHandler
{
    private readonly BookContext _context;
    private readonly IValidator<GetOrderByIdRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrderByIdHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public GetOrderByIdHandler(
        BookContext context,
        IValidator<GetOrderByIdRequest> validator,
        IMapper mapper,
        ILogger<GetOrderByIdHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<Ok<OrderProfileDto>, ValidationProblem, NotFound>> Handle(
        GetOrderByIdRequest request,
        HttpContext httpContext)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation(
            new EventId(LogEvents.OrderRetrievalStarted, nameof(LogEvents.OrderRetrievalStarted)),
            "Order retrieval started - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
            operationId, request.Id, traceId);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                new EventId(LogEvents.ValidationFailed, nameof(LogEvents.ValidationFailed)),
                "Validation failed for order ID - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            
            var extensions = new Dictionary<string, object?> { ["traceId"] = traceId };
            return TypedResults.ValidationProblem(validationResult.ToDictionary(), extensions: extensions);
        }

        // Check cache
        var cacheKey = $"order_{request.Id}";
        var cachedOrder = _cacheService.GetCachedOrder<OrderProfileDto>(cacheKey);
        
        if (cachedOrder != null)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                new EventId(LogEvents.CacheHit, nameof(LogEvents.CacheHit)),
                "Cache hit - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}, Duration: {Duration}ms",
                operationId, request.Id, traceId, stopwatch.ElapsedMilliseconds);
            return TypedResults.Ok(cachedOrder);
        }

        _logger.LogDebug(
            new EventId(LogEvents.CacheMiss, nameof(LogEvents.CacheMiss)),
            "Cache miss - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
            operationId, request.Id, traceId);

        // Parse ID
        if (!Guid.TryParse(request.Id, out var orderId))
        {
            _logger.LogWarning(
                new EventId(LogEvents.OrderNotFound, nameof(LogEvents.OrderNotFound)),
                "Invalid order ID format - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            return TypedResults.NotFound();
        }

        // Query database
        _logger.LogDatabaseOperationStarted(operationId, "GetOrderById");
        var dbStopwatch = Stopwatch.StartNew();
        
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        dbStopwatch.Stop();
        _logger.LogDatabaseOperationCompleted(operationId, "GetOrderById", orderId.ToString(), dbStopwatch.ElapsedMilliseconds);

        if (order == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                new EventId(LogEvents.OrderNotFound, nameof(LogEvents.OrderNotFound)),
                "Order not found - OperationId: {OperationId}, OrderId: {OrderId}, Duration: {Duration}ms",
                operationId, request.Id, stopwatch.ElapsedMilliseconds);
            return TypedResults.NotFound();
        }

        var orderDto = _mapper.Map<OrderProfileDto>(order);

        // Cache the result
        _logger.LogCacheOperation(operationId, "CacheOrder", cacheKey);
        _cacheService.CacheOrder(cacheKey, orderDto);

        stopwatch.Stop();
        _logger.LogInformation(
            new EventId(LogEvents.OrderRetrievalCompleted, nameof(LogEvents.OrderRetrievalCompleted)),
            "Order retrieved from database - OperationId: {OperationId}, OrderId: {OrderId}, Title: {Title}, Duration: {Duration}ms",
            operationId, request.Id, order.Title, stopwatch.ElapsedMilliseconds);
            
        return TypedResults.Ok(orderDto);
    }
}

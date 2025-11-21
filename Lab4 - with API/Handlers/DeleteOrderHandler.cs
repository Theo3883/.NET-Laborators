using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Logging;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;

namespace Lab3.Handlers;

public class DeleteOrderHandler
{
    private readonly BookContext _context;
    private readonly IValidator<DeleteOrderRequest> _validator;
    private readonly ILogger<DeleteOrderHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public DeleteOrderHandler(
        BookContext context,
        IValidator<DeleteOrderRequest> validator,
        ILogger<DeleteOrderHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        DeleteOrderRequest request,
        HttpContext httpContext)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation(
            new EventId(LogEvents.OrderDeleteStarted, nameof(LogEvents.OrderDeleteStarted)),
            "Order deletion started - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
            operationId, request.Id, traceId);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                new EventId(LogEvents.ValidationFailed, nameof(LogEvents.ValidationFailed)),
                "Validation failed for order deletion - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            
            var extensions = new Dictionary<string, object?> { ["traceId"] = traceId };
            return TypedResults.ValidationProblem(validationResult.ToDictionary(), extensions: extensions);
        }

        // Parse ID
        if (!Guid.TryParse(request.Id, out var orderId))
        {
            _logger.LogWarning(
                new EventId(LogEvents.OrderNotFound, nameof(LogEvents.OrderNotFound)),
                "Invalid order ID format - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            return TypedResults.NotFound();
        }

        // Find order
        _logger.LogDatabaseOperationStarted(operationId, "FindOrder");
        var order = await _context.Orders.FindAsync(orderId);
        
        if (order == null)
        {
            _logger.LogWarning(
                new EventId(LogEvents.OrderNotFound, nameof(LogEvents.OrderNotFound)),
                "Order not found for deletion - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            return TypedResults.NotFound();
        }

        // Delete order
        _logger.LogDatabaseOperationStarted(operationId, "DeleteOrder");
        var dbStopwatch = Stopwatch.StartNew();
        
        // Track category before deletion for cache invalidation
        var orderCategory = order.Category;
        
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        
        dbStopwatch.Stop();
        _logger.LogDatabaseOperationCompleted(operationId, "DeleteOrder", order.Id.ToString(), dbStopwatch.ElapsedMilliseconds);

        // Category-based cache invalidation: Only invalidate the affected category
        _logger.LogCacheOperation(operationId, "InvalidateCategoryCache", orderCategory.ToString());
        _cacheService.InvalidateCategoryCache(orderCategory);
        
        // Also invalidate global cache and specific order cache
        _cacheService.InvalidateOrderCache("orders_all");
        _cacheService.InvalidateOrderCache($"order_{order.Id}");

        stopwatch.Stop();
        _logger.LogInformation(
            new EventId(LogEvents.OrderDeleteCompleted, nameof(LogEvents.OrderDeleteCompleted)),
            "Order deleted successfully - OperationId: {OperationId}, OrderId: {OrderId}, Title: {Title}, Duration: {Duration}ms",
            operationId, request.Id, order.Title, stopwatch.ElapsedMilliseconds);
            
        return TypedResults.NoContent();
    }
}

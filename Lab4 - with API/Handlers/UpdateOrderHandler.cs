using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Logging;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Lab3.Handlers;

public class UpdateOrderHandler
{
    private readonly BookContext _context;
    private readonly IValidator<UpdateOrderRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateOrderHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public UpdateOrderHandler(
        BookContext context,
        IValidator<UpdateOrderRequest> validator,
        IMapper mapper,
        ILogger<UpdateOrderHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<Ok<OrderProfileDto>, ValidationProblem, NotFound, Conflict<object>>> Handle(
        UpdateOrderRequest request, 
        HttpContext httpContext)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation(
            new EventId(LogEvents.OrderUpdateStarted, nameof(LogEvents.OrderUpdateStarted)),
            "Order update started - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
            operationId, request.Id, traceId);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                new EventId(LogEvents.ValidationFailed, nameof(LogEvents.ValidationFailed)),
                "Validation failed for order update - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
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

        // Check if order exists
        _logger.LogDatabaseOperationStarted(operationId, "FindOrder");
        var order = await _context.Orders.FindAsync(orderId);
        
        if (order == null)
        {
            _logger.LogWarning(
                new EventId(LogEvents.OrderNotFound, nameof(LogEvents.OrderNotFound)),
                "Order not found for update - OperationId: {OperationId}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.Id, traceId);
            return TypedResults.NotFound();
        }

        // ISBN uniqueness check (excluding current order)
        var isbnStopwatch = Stopwatch.StartNew();
        var isbnExists = await _context.Orders
            .AnyAsync(o => o.ISBN == request.ISBN && o.Id != orderId);
        isbnStopwatch.Stop();
        
        _logger.LogISBNValidation(operationId, request.ISBN, !isbnExists, isbnStopwatch.ElapsedMilliseconds);

        if (isbnExists)
        {
            _logger.LogWarning(
                new EventId(LogEvents.OrderUpdateFailed, nameof(LogEvents.OrderUpdateFailed)),
                "ISBN conflict on update - OperationId: {OperationId}, ISBN: {ISBN}, OrderId: {OrderId}, TraceId: {TraceId}",
                operationId, request.ISBN, request.Id, traceId);
            return TypedResults.Conflict((object)new 
            { 
                message = $"An order with ISBN '{request.ISBN}' already exists",
                traceId = traceId
            });
        }

        // Stock validation
        _logger.LogStockValidation(operationId, request.StockQuantity, request.StockQuantity >= 0);

        // Update order properties
        _logger.LogDatabaseOperationStarted(operationId, "UpdateOrder");
        var dbStopwatch = Stopwatch.StartNew();
        
        // Track old category for cache invalidation
        var oldCategory = order.Category;
        
        order.Title = request.Title;
        order.Author = request.Author;
        order.ISBN = request.ISBN;
        order.Category = Enum.Parse<OrderCategory>(request.Category, ignoreCase: true);
        order.Price = request.Price;
        order.PublishedDate = request.PublishedDate;
        order.StockQuantity = request.StockQuantity;
        order.CoverImageUrl = request.CoverImageUrl;

        await _context.SaveChangesAsync();
        dbStopwatch.Stop();
        
        _logger.LogDatabaseOperationCompleted(operationId, "UpdateOrder", order.Id.ToString(), dbStopwatch.ElapsedMilliseconds);

        // Category-based cache invalidation: Invalidate both old and new categories if changed
        if (oldCategory != order.Category)
        {
            _logger.LogCacheOperation(operationId, "InvalidateCategoryCache", $"old_{oldCategory}_new_{order.Category}");
            _cacheService.InvalidateCategoryCache(oldCategory);
            _cacheService.InvalidateCategoryCache(order.Category);
        }
        else
        {
            _logger.LogCacheOperation(operationId, "InvalidateCategoryCache", order.Category.ToString());
            _cacheService.InvalidateCategoryCache(order.Category);
        }
        
        // Also invalidate global cache
        _cacheService.InvalidateOrderCache("orders_all");
        _cacheService.InvalidateOrderCache($"order_{order.Id}");

        var orderDto = _mapper.Map<OrderProfileDto>(order);

        stopwatch.Stop();
        _logger.LogInformation(
            new EventId(LogEvents.OrderUpdateCompleted, nameof(LogEvents.OrderUpdateCompleted)),
            "Order updated successfully - OperationId: {OperationId}, OrderId: {OrderId}, Title: {Title}, Duration: {Duration}ms",
            operationId, order.Id, order.Title, stopwatch.ElapsedMilliseconds);
            
        return TypedResults.Ok(orderDto);
    }
}

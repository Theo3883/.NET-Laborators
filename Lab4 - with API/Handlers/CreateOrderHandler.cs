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

public class CreateOrderHandler
{
    private readonly BookContext _context;
    private readonly IValidator<CreateOrderProfileRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IOrderCacheService _cacheService;

    public CreateOrderHandler(
        BookContext context,
        IValidator<CreateOrderProfileRequest> validator,
        IMapper mapper,
        ILogger<CreateOrderHandler> logger,
        IOrderCacheService cacheService)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Results<Created<OrderProfileDto>, ValidationProblem, Conflict<object>>> Handle(
        CreateOrderProfileRequest request, 
        HttpContext httpContext)
    {
        // Generate unique 8-character operation ID for tracking
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var totalStopwatch = Stopwatch.StartNew();
        var traceId = httpContext.TraceIdentifier;
        
        // Parse category early for logging purposes
        Enum.TryParse<OrderCategory>(request.Category, true, out var category);
        
        // Create logging scope for entire operation (correlation)
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["TraceId"] = traceId,
            ["OrderTitle"] = request.Title,
            ["ISBN"] = request.ISBN,
            ["Category"] = category
        });

        // Log operation start with order-specific details
        _logger.LogOrderCreationStarted(operationId, request.Title, request.Author, request.ISBN, category);

        TimeSpan validationDuration = TimeSpan.Zero;
        TimeSpan databaseDuration = TimeSpan.Zero;
        string? errorReason = null;

        try
        {
            // Validation phase - FluentValidation rules
            var validationStopwatch = Stopwatch.StartNew();
            var validationResult = await _validator.ValidateAsync(request);
            validationStopwatch.Stop();
            validationDuration = validationStopwatch.Elapsed;

            if (!validationResult.IsValid)
            {
                errorReason = $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}";
                
                // Log comprehensive metrics for validation failure
                _logger.LogOrderCreationMetrics(new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = category,
                    ValidationDuration = validationDuration,
                    DatabaseSaveDuration = databaseDuration,
                    TotalDuration = totalStopwatch.Elapsed,
                    Success = false,
                    ErrorReason = errorReason
                });

                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                // Add TraceId to extensions for tracking
                var extensions = new Dictionary<string, object?>
                {
                    ["traceId"] = traceId
                };

                return TypedResults.ValidationProblem(errors, extensions: extensions);
            }

            // ISBN uniqueness validation with performance tracking
            var isbnValidationStopwatch = Stopwatch.StartNew();
            var isbnExists = await _context.Orders.AnyAsync(o => o.ISBN == request.ISBN);
            isbnValidationStopwatch.Stop();
            
            // Log ISBN validation with ISBNValidationPerformed event
            _logger.LogISBNValidation(
                operationId, 
                request.ISBN, 
                !isbnExists, 
                isbnValidationStopwatch.ElapsedMilliseconds);

            if (isbnExists)
            {
                errorReason = $"Duplicate ISBN: {request.ISBN}";
                
                // Log metrics for ISBN conflict
                _logger.LogOrderCreationMetrics(new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = category,
                    ValidationDuration = validationDuration,
                    DatabaseSaveDuration = databaseDuration,
                    TotalDuration = totalStopwatch.Elapsed,
                    Success = false,
                    ErrorReason = errorReason
                });

                return TypedResults.Conflict((object)new
                {
                    message = $"An order with ISBN '{request.ISBN}' already exists.",
                    isbn = request.ISBN,
                    traceId = traceId
                });
            }

            // Stock quantity validation with StockValidationPerformed event
            _logger.LogStockValidation(
                operationId, 
                request.StockQuantity, 
                request.StockQuantity >= 0);

            // Database operation with performance tracking
            _logger.LogDatabaseOperationStarted(operationId, "CreateOrder");
            
            var dbStopwatch = Stopwatch.StartNew();
            
            // Map request to entity using AutoMapper
            var order = _mapper.Map<Order>(request);
            
            // Add to context and persist
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            
            dbStopwatch.Stop();
            databaseDuration = dbStopwatch.Elapsed;

            // Log database operation completion with OrderId
            _logger.LogDatabaseOperationCompleted(
                operationId, 
                "CreateOrder", 
                order.Id.ToString(), 
                dbStopwatch.ElapsedMilliseconds);

            // Cache invalidation with "all_orders" cache key logging
            _logger.LogCacheOperation(operationId, "InvalidateAllOrderCaches", "orders_all");
            _cacheService.InvalidateAllOrderCaches();

            // Map to DTO with custom resolvers (conditional price, cover image, etc.)
            var orderDto = _mapper.Map<OrderProfileDto>(order);

            totalStopwatch.Stop();

            // Log comprehensive OrderCreationMetrics for successful operation
            _logger.LogOrderCreationMetrics(new OrderCreationMetrics
            {
                OperationId = operationId,
                OrderTitle = request.Title,
                ISBN = request.ISBN,
                Category = category,
                ValidationDuration = validationDuration,
                DatabaseSaveDuration = databaseDuration,
                TotalDuration = totalStopwatch.Elapsed,
                Success = true,
                ErrorReason = null
            });

            return TypedResults.Created($"/orders/{order.Id}", orderDto);
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            errorReason = $"Exception: {ex.Message}";
            
            // Log error metrics in catch block with order details
            _logger.LogOrderCreationMetrics(new OrderCreationMetrics
            {
                OperationId = operationId,
                OrderTitle = request.Title,
                ISBN = request.ISBN,
                Category = category,
                ValidationDuration = validationDuration,
                DatabaseSaveDuration = databaseDuration,
                TotalDuration = totalStopwatch.Elapsed,
                Success = false,
                ErrorReason = errorReason
            });

            // Re-throw exception for global exception handler
            throw;
        }
    }
}

using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
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

    public async Task<Results<Created<OrderProfileDto>, ValidationProblem, Conflict<object>>> Handle(CreateOrderProfileRequest request)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Order creation operation started - OperationId: {OperationId}, Title: {Title}, Author: {Author}, ISBN: {ISBN}, Category: {Category}",
            operationId, request.Title, request.Author, request.ISBN, request.Category);

        try
        {
            // Validation
            var validationStopwatch = Stopwatch.StartNew();
            var validationResult = await _validator.ValidateAsync(request);
            validationStopwatch.Stop();

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validation failed for order creation - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Errors: {Errors}",
                    operationId, request.Title, request.ISBN, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                _logger.LogError(
                    "Order creation failed - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Category: {Category}, ValidationDuration: {ValidationDuration}ms, DatabaseSaveDuration: {DbDuration}ms, TotalDuration: {TotalDuration}ms, Success: {Success}, ErrorReason: {ErrorReason}",
                    operationId, request.Title, request.ISBN, request.Category,
                    validationStopwatch.ElapsedMilliseconds, 0, stopwatch.ElapsedMilliseconds, false, "Validation failed");

                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return TypedResults.ValidationProblem(errors);
            }

            // Check ISBN uniqueness
            var isbnExists = await _context.Orders.AnyAsync(o => o.ISBN == request.ISBN);
            if (isbnExists)
            {
                _logger.LogWarning(
                    "Duplicate ISBN found - OperationId: {OperationId}, ISBN: {ISBN}",
                    operationId, request.ISBN);

                return TypedResults.Conflict((object)new
                {
                    message = $"An order with ISBN '{request.ISBN}' already exists.",
                    isbn = request.ISBN
                });
            }

            // Map and create order using advanced mapping
            var dbStopwatch = Stopwatch.StartNew();
            var order = _mapper.Map<Order>(request);
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            dbStopwatch.Stop();

            _logger.LogInformation(
                "Database operation completed - OperationId: {OperationId}, OrderId: {OrderId}, Title: {Title}, ISBN: {ISBN}, Duration: {Duration}ms",
                operationId, order.Id, request.Title, request.ISBN, dbStopwatch.ElapsedMilliseconds);

            // Invalidate all orders cache
            _cacheService.InvalidateAllOrderCaches();
            _logger.LogInformation(
                "Cache invalidation performed - OperationId: {OperationId}",
                operationId);

            // Map to DTO with custom resolvers
            var orderDto = _mapper.Map<OrderProfileDto>(order);

            stopwatch.Stop();

            _logger.LogInformation(
                "Order creation completed successfully - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Category: {Category}, ValidationDuration: {ValidationDuration}ms, DatabaseSaveDuration: {DbDuration}ms, TotalDuration: {TotalDuration}ms, Success: {Success}",
                operationId, request.Title, request.ISBN, request.Category,
                validationStopwatch.ElapsedMilliseconds, dbStopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds, true);

            return TypedResults.Created($"/orders/{order.Id}", orderDto);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Error creating order - OperationId: {OperationId}, Title: {Title}, Author: {Author}, ISBN: {ISBN}",
                operationId, request.Title, request.Author, request.ISBN);

            throw;
        }
    }
}

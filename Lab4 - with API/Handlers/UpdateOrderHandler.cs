using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Results<Ok<OrderProfileDto>, ValidationProblem, NotFound, Conflict<object>>> Handle(UpdateOrderRequest request)
    {
        _logger.LogInformation("Updating order: {OrderId}", request.Id);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for order update: {OrderId}", request.Id);
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        // Parse ID
        if (!Guid.TryParse(request.Id, out var orderId))
        {
            _logger.LogWarning("Invalid order ID format: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        // Check if order exists
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for update: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        // Check ISBN uniqueness (excluding current order)
        var isbnExists = await _context.Orders
            .AnyAsync(o => o.ISBN == request.ISBN && o.Id != orderId);

        if (isbnExists)
        {
            _logger.LogWarning("ISBN conflict on update - ISBN: {ISBN}, OrderId: {OrderId}", request.ISBN, request.Id);
            return TypedResults.Conflict((object)new { message = $"An order with ISBN '{request.ISBN}' already exists" });
        }

        // Update order properties
        order.Title = request.Title;
        order.Author = request.Author;
        order.ISBN = request.ISBN;
        order.Category = Enum.Parse<OrderCategory>(request.Category, ignoreCase: true);
        order.Price = request.Price;
        order.PublishedDate = request.PublishedDate;
        order.StockQuantity = request.StockQuantity;
        order.CoverImageUrl = request.CoverImageUrl;

        await _context.SaveChangesAsync();

        // Invalidate all caches
        _cacheService.InvalidateAllOrderCaches();

        var orderDto = _mapper.Map<OrderProfileDto>(order);

        _logger.LogInformation("Order updated successfully: {OrderId}, Title: {Title}", order.Id, order.Title);
        return TypedResults.Ok(orderDto);
    }
}

using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;

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

    public async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(DeleteOrderRequest request)
    {
        _logger.LogInformation("Deleting order: {OrderId}", request.Id);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for order deletion: {OrderId}", request.Id);
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        // Parse ID
        if (!Guid.TryParse(request.Id, out var orderId))
        {
            _logger.LogWarning("Invalid order ID format: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        // Find order
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for deletion: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        // Delete order
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        // Invalidate all caches
        _cacheService.InvalidateAllOrderCaches();

        _logger.LogInformation("Order deleted successfully: {OrderId}, Title: {Title}", request.Id, order.Title);
        return TypedResults.NoContent();
    }
}

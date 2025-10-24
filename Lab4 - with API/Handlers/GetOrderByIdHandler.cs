using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Results<Ok<OrderProfileDto>, ValidationProblem, NotFound>> Handle(GetOrderByIdRequest request)
    {
        _logger.LogInformation("Getting order by ID: {OrderId}", request.Id);

        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for order ID: {OrderId}", request.Id);
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        // Check cache
        var cacheKey = $"order_{request.Id}";
        var cachedOrder = _cacheService.GetCachedOrder<OrderProfileDto>(cacheKey);
        if (cachedOrder != null)
        {
            _logger.LogInformation("Retrieved order from cache: {OrderId}", request.Id);
            return TypedResults.Ok(cachedOrder);
        }

        // Parse ID
        if (!Guid.TryParse(request.Id, out var orderId))
        {
            _logger.LogWarning("Invalid order ID format: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        // Query database
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order not found: {OrderId}", request.Id);
            return TypedResults.NotFound();
        }

        var orderDto = _mapper.Map<OrderProfileDto>(order);

        // Cache the result
        _cacheService.CacheOrder(cacheKey, orderDto);

        _logger.LogInformation("Retrieved order from database: {OrderId}", request.Id);
        return TypedResults.Ok(orderDto);
    }
}

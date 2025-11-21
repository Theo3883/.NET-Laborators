using Lab3.DTO;
using Lab3.Model;
using Lab3.Models;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Lab3.Handlers;

public class GetLocalizedOrdersHandler
{
    private readonly BookContext _context;
    private readonly IOrderLocalizationService _localizationService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetLocalizedOrdersHandler> _logger;

    public GetLocalizedOrdersHandler(
        BookContext context,
        IOrderLocalizationService localizationService,
        IMapper mapper,
        ILogger<GetLocalizedOrdersHandler> logger)
    {
        _context = context;
        _localizationService = localizationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Results<Ok<List<OrderProfileDto>>, BadRequest<string>>> HandleAsync(
        string? culture, 
        HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        var requestedCulture = culture ?? "en-US";

        _logger.LogInformation(
            "Retrieving localized orders - Culture: {Culture}, TraceId: {TraceId}",
            requestedCulture,
            traceId);

        // Validate culture
        if (!_localizationService.IsCultureSupported(requestedCulture))
        {
            var supportedCultures = string.Join(", ", _localizationService.GetSupportedCultures());
            _logger.LogWarning(
                "Unsupported culture requested: {Culture}, Supported: {SupportedCultures}, TraceId: {TraceId}",
                requestedCulture,
                supportedCultures,
                traceId);
            
            return TypedResults.BadRequest(
                $"Culture '{requestedCulture}' is not supported. Supported cultures: {supportedCultures}");
        }

        // Fetch all orders
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Map to DTOs with localization
        var localizedOrders = orders.Select(order =>
        {
            var dto = _mapper.Map<OrderProfileDto>(order);
            
            // Add localized information
            dto.Culture = requestedCulture;
            dto.LocalizedCategoryName = _localizationService.GetLocalizedCategoryName(order.Category, requestedCulture);
            dto.LocalizedCategoryDescription = _localizationService.GetLocalizedCategoryDescription(order.Category, requestedCulture);
            
            // Also update the CategoryDisplayName to use localized version
            dto.CategoryDisplayName = dto.LocalizedCategoryName;
            
            // Localize availability status
            var statusKey = order.StockQuantity == 0 ? "OutOfStock" :
                           order.StockQuantity < 20 ? "LowStock" : "InStock";
            dto.AvailabilityStatus = _localizationService.GetLocalizedTerm(statusKey, requestedCulture);
            
            return dto;
        }).ToList();

        _logger.LogInformation(
            "Retrieved {Count} localized orders in culture {Culture}, TraceId: {TraceId}",
            localizedOrders.Count,
            requestedCulture,
            traceId);

        return TypedResults.Ok(localizedOrders);
    }
}

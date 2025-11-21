using Lab3.DTO.Response;
using Lab3.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lab3.Handlers;

public class GetOrderMetricsHandler
{
    private readonly IOrderMetricsService _metricsService;
    private readonly ILogger<GetOrderMetricsHandler> _logger;

    public GetOrderMetricsHandler(
        IOrderMetricsService metricsService,
        ILogger<GetOrderMetricsHandler> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task<Results<Ok<OrderMetricsDto>, ProblemHttpResult>> HandleAsync(HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        try
        {
            _logger.LogInformation(
                "Retrieving order metrics dashboard - TraceId: {TraceId}",
                traceId);

            var metrics = await _metricsService.GetOrderMetricsAsync();

            _logger.LogInformation(
                "Order metrics retrieved successfully - TotalOrders: {TotalOrders}, " +
                "TotalRevenue: {TotalRevenue}, CacheKeys: {CacheKeys}, TraceId: {TraceId}",
                metrics.OrderCreation.TotalOrders,
                metrics.OrderCreation.FormattedTotalRevenue,
                metrics.Performance.CachePerformance.TotalCacheKeys,
                traceId);

            return TypedResults.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving order metrics - TraceId: {TraceId}, Error: {ErrorMessage}",
                traceId,
                ex.Message);

            return TypedResults.Problem(
                title: "Error retrieving metrics",
                detail: "An error occurred while retrieving order metrics. Please try again.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

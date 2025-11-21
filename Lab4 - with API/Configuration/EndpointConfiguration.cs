using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.DTO.Response;
using Lab3.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Lab3.Configuration;

/// Configures API endpoints 
public static class EndpointConfiguration
{
    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
        app.MapOrderCrudEndpoints();

        return app;
    }

    private static void MapOrderCrudEndpoints(this WebApplication app)
    {
        // CREATE
        app.MapPost("/orders", async ([FromBody] CreateOrderProfileRequest request, 
            HttpContext httpContext,
            [FromServices] CreateOrderHandler handler) =>
        {
            return await handler.Handle(request, httpContext);
        })
        .WithName("CreateOrder")
        .WithDescription("Create a new order with advanced validation, AutoMapper, and business rules")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status409Conflict);

        // GET BY ID
        app.MapGet("/orders/{id}", async (
            string id, 
            HttpContext httpContext,
            [FromServices] GetOrderByIdHandler handler) =>
        {
            return await handler.Handle(new GetOrderByIdRequest(id), httpContext);
        })
        .WithName("GetOrderById")
        .WithDescription("Retrieve a specific order by its ID")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        // GET ALL
        app.MapGet("/orders", async (
            HttpContext httpContext,
            [FromServices] GetAllOrdersHandler handler) =>
        {
            return await handler.Handle(new GetAllOrdersRequest(), httpContext);
        })
        .WithName("GetAllOrders")
        .WithDescription("Retrieve all orders")
        .WithTags("Orders")
        .Produces<List<OrderProfileDto>>(StatusCodes.Status200OK);

        // GET BY CATEGORY with category-based caching
        app.MapGet("/orders/category/{category}", async (
            string category,
            HttpContext httpContext,
            [FromServices] GetOrdersByCategoryHandler handler) =>
        {
            return await handler.Handle(category, httpContext);
        })
        .WithName("GetOrdersByCategory")
        .WithDescription("Retrieve orders filtered by category (Fiction, NonFiction, Technical, Children) with category-specific caching")
        .WithTags("Orders")
        .Produces<List<OrderProfileDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        // GET WITH PAGINATION
        app.MapGet("/orders/paginated", async (
            [FromQuery] int page, 
            [FromQuery] int pageSize,
            HttpContext httpContext,
            [FromServices] GetOrdersWithPaginationHandler handler) =>
        {
            return await handler.Handle(new GetOrdersWithPaginationRequest(page, pageSize), httpContext);
        })
        .WithName("GetOrdersPaginated")
        .WithDescription("Retrieve orders with pagination support")
        .WithTags("Orders")
        .Produces<PagedResult<OrderProfileDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        // UPDATE
        app.MapPut("/orders/{id}", async (
            string id,
            [FromBody] UpdateOrderRequest request,
            HttpContext httpContext,
            [FromServices] UpdateOrderHandler handler) =>
        {
            // Ensure ID from route matches request body
            var updatedRequest = request with { Id = id };
            return await handler.Handle(updatedRequest, httpContext);
        })
        .WithName("UpdateOrder")
        .WithDescription("Update an existing order")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // DELETE
        app.MapDelete("/orders/{id}", async (
            string id,
            HttpContext httpContext,
            [FromServices] DeleteOrderHandler handler) =>
        {
            return await handler.Handle(new DeleteOrderRequest(id), httpContext);
        })
        .WithName("DeleteOrder")
        .WithDescription("Delete an order by its ID")
        .WithTags("Orders")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);
        
        // METRICS DASHBOARD
        app.MapGet("/orders/metrics", async (
            HttpContext httpContext,
            [FromServices] GetOrderMetricsHandler handler) =>
        {
            return await handler.HandleAsync(httpContext);
        })
        .WithName("GetOrderMetrics")
        .WithDescription("Get comprehensive order metrics dashboard including creation stats, inventory metrics, category breakdown, and real-time performance data")
        .WithTags("Orders", "Metrics")
        .Produces<OrderMetricsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);
        
        // LOCALIZED ORDERS
        app.MapGet("/orders/localized", async (
            HttpContext httpContext,
            [FromQuery] string? culture,
            [FromServices] GetLocalizedOrdersHandler handler) =>
        {
            return await handler.HandleAsync(culture, httpContext);
        })
        .WithName("GetLocalizedOrders")
        .WithDescription("Get all orders with localized category names and descriptions. Supports en-US, fr-FR, es-ES, de-DE. Use ?culture=fr-FR query parameter.")
        .WithTags("Orders", "Localization")
        .Produces<List<OrderProfileDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }
}

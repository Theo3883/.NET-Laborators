using Lab3.DTO;
using Lab3.DTO.Request;
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
            [FromServices] CreateOrderHandler handler) =>
        {
            return await handler.Handle(request);
        })
        .WithName("CreateOrder")
        .WithDescription("Create a new order with advanced validation, AutoMapper, and business rules")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status409Conflict);

        // GET BY ID
        app.MapGet("/orders/{id}", async (string id, [FromServices] GetOrderByIdHandler handler) =>
        {
            return await handler.Handle(new GetOrderByIdRequest(id));
        })
        .WithName("GetOrderById")
        .WithDescription("Retrieve a specific order by its ID")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        // GET ALL
        app.MapGet("/orders", async ([FromServices] GetAllOrdersHandler handler) =>
        {
            return await handler.Handle(new GetAllOrdersRequest());
        })
        .WithName("GetAllOrders")
        .WithDescription("Retrieve all orders")
        .WithTags("Orders")
        .Produces<List<OrderProfileDto>>(StatusCodes.Status200OK);

        // GET WITH PAGINATION
        app.MapGet("/orders/paginated", async (
            [FromQuery] int page, 
            [FromQuery] int pageSize,
            [FromServices] GetOrdersWithPaginationHandler handler) =>
        {
            return await handler.Handle(new GetOrdersWithPaginationRequest(page, pageSize));
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
            [FromServices] UpdateOrderHandler handler) =>
        {
            // Ensure ID from route matches request body
            var updatedRequest = request with { Id = id };
            return await handler.Handle(updatedRequest);
        })
        .WithName("UpdateOrder")
        .WithDescription("Update an existing order")
        .WithTags("Orders")
        .Produces<OrderProfileDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // DELETE
        app.MapDelete("/orders/{id}", async (string id, [FromServices] DeleteOrderHandler handler) =>
        {
            return await handler.Handle(new DeleteOrderRequest(id));
        })
        .WithName("DeleteOrder")
        .WithDescription("Delete an order by its ID")
        .WithTags("Orders")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);
    }
}

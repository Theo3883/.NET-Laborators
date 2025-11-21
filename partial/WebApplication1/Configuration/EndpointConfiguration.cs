using WebApplication1.DTO.Request;
using WebApplication1.Handlers;
using WebApplication1.Model;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Configuration;

/// <summary>
/// Configures API endpoints
/// </summary>
public static class EndpointConfiguration
{
    public static WebApplication MapPaperEndpoints(this WebApplication app)
    {
        app.MapPaperCrudEndpoints();

        return app;
    }

    private static void MapPaperCrudEndpoints(this WebApplication app)
    {
        // CREATE
        app.MapPost("/papers", async ([FromBody] CreatePaperRequest request, 
            HttpContext httpContext,
            [FromServices] CreatePaperHandler handler) =>
        {
            return await handler.Handle(request, httpContext);
        })
        .WithName("CreatePaper")
        .WithDescription("Create a new paper")
        .WithTags("Papers")
        .Produces<Paper>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // GET BY ID
        app.MapGet("/papers/{id}", async (
            int id, 
            HttpContext httpContext,
            [FromServices] GetPaperByIdHandler handler) =>
        {
            return await handler.Handle(new GetPaperByIdRequest(id), httpContext);
        })
        .WithName("GetPaperById")
        .WithDescription("Retrieve a specific paper by its ID")
        .WithTags("Papers")
        .Produces<Paper>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET ALL
        app.MapGet("/papers", async (
            HttpContext httpContext,
            [FromServices] GetAllPapersHandler handler) =>
        {
            return await handler.Handle(httpContext);
        })
        .WithName("GetAllPapers")
        .WithDescription("Retrieve all papers")
        .WithTags("Papers")
        .Produces<List<Paper>>(StatusCodes.Status200OK);

        // GET TOP 3 MOST RECENT
        app.MapGet("/papers/top3", async (
            HttpContext httpContext,
            [FromServices] GetTop3PapersHandler handler) =>
        {
            return await handler.Handle(httpContext);
        })
        .WithName("GetTop3Papers")
        .WithDescription("Get the top 3 most recent papers ordered by date")
        .WithTags("Papers")
        .Produces<List<Paper>>(StatusCodes.Status200OK);
    }
}


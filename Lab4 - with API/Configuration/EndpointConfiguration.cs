using AutoMapper;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Handlers;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Configuration;


/// Configures API endpoints 
public static class EndpointConfiguration
{
    public static WebApplication MapBookEndpoints(this WebApplication app)
    {
        app.MapBookCrudEndpoints();
        app.MapBookBatchEndpoints();
        app.MapBookQueryEndpoints();
        app.MapBookLocalizationEndpoints();
        app.MapMetadataEndpoints();
        app.MapCacheEndpoints();
        app.MapMetricsEndpoints();

        return app;
    }

    private static void MapBookCrudEndpoints(this WebApplication app)
    {
        app.MapPost("/books", async ([FromBody] CreateBookProfileRequest request, 
            [FromServices] CreateBookHandler handler) =>
        {
            return await handler.Handle(request);
        })
        .WithName("CreateBook")
        .WithDescription("Create a new book with advanced validation, AutoMapper, and business rules")
        .WithTags("Books")
        .Produces<BookProfileDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status409Conflict);

        app.MapGet("/books/{id:guid}", async (Guid id, [FromServices] GetBookByIdHandler handler) =>
        {
            var request = new GetBookByIdRequest(id);
            return await handler.Handle(request);
        })
        .WithName("GetBookById")
        .WithDescription("Get a specific book by ID")
        .WithTags("Books")
        .Produces<BookProfileDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPut("/books/{id:guid}", async (Guid id, [FromBody] UpdateBookRequest request, 
            [FromServices] UpdateBookHandler handler) =>
        {
            if (id != request.Id)
            {
                return Results.BadRequest("ID in URL does not match ID in request body.");
            }
            return await handler.Handle(request);
        })
        .WithName("UpdateBook")
        .WithDescription("Update an existing book")
        .WithTags("Books")
        .Produces<BookProfileDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/books/{id:guid}", async (Guid id, [FromServices] DeleteBookHandler handler) =>
        {
            var request = new DeleteBookRequest(id);
            return await handler.Handle(request);
        })
        .WithName("DeleteBook")
        .WithDescription("Delete a book by ID")
        .WithTags("Books")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static void MapBookQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/books", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] int? category,
            [FromServices] GetBooksWithPaginationHandler handler) =>
        {
            var categoryEnum = category.HasValue ? (BookCategory?)category.Value : null;
            var request = new GetBooksWithPaginationRequest(page, pageSize, categoryEnum);
            return await handler.Handle(request);
        })
        .WithName("GetBooksPaginated")
        .WithDescription("Get paginated list of books with optional category filter and caching. Category: 0=Fiction, 1=NonFiction, 2=Technical, 3=Children")
        .WithTags("Books")
        .Produces(StatusCodes.Status200OK);

        app.MapGet("/books-all", async (BookContext context, [FromServices] IMapper mapper) =>
        {
            var books = await context.Books.ToListAsync();
            var bookDtos = mapper.Map<List<BookProfileDto>>(books);
            return Results.Ok(bookDtos);
        })
        .WithName("GetAllBooks")
        .WithDescription("Get all books without pagination")
        .WithTags("Books")
        .Produces<List<BookProfileDto>>(StatusCodes.Status200OK);
    }

    private static void MapBookBatchEndpoints(this WebApplication app)
    {
        app.MapPost("/books/batch", async ([FromBody] BatchCreateBooksRequest request, 
            [FromServices] BatchCreateBooksHandler handler) =>
        {
            return await handler.Handle(request);
        })
        .WithName("BatchCreateBooks")
        .WithDescription("Create multiple books in a single transaction with parallel processing (max 100 books)")
        .WithTags("Books", "Batch")
        .Produces<BatchCreateBooksResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static void MapBookLocalizationEndpoints(this WebApplication app)
    {
        app.MapGet("/books/{id:guid}/localized",
            async (Guid id, [FromQuery] string? culture, 
                [FromServices] IBookLocalizationService localizationService,
                [FromServices] IBookMetadataService metadataService, 
                [FromServices] BookContext context) =>
        {
            var cultureCode = culture ?? "en-US";

            var book = await context.Books
                .Include(b => b.Localizations)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return Results.NotFound(new { message = $"Book with ID {id} not found" });

            var localizedTitle = await localizationService.GetLocalizedTitleAsync(id, cultureCode);
            var localizedDescription = await localizationService.GetLocalizedDescriptionAsync(id, cultureCode);
            var categoryName = metadataService.GetCategoryName(book.Category, cultureCode);
            var categoryDescription = metadataService.GetCategoryDescription(book.Category, cultureCode);
            var availabilityStatus = metadataService.GetAvailabilityStatus(book.StockQuantity, cultureCode);

            var availableCultures = book.Localizations.Select(l => l.CultureCode).ToList();
            if (!availableCultures.Contains("en-US"))
                availableCultures.Insert(0, "en-US");

            var result = new
            {
                id = book.Id,
                title = localizedTitle,
                description = localizedDescription,
                author = book.Author,
                isbn = book.ISBN,
                category = categoryName,
                categoryDescription,
                price = book.Price,
                formattedPrice = book.Price.ToString("C"),
                publishedDate = book.PublishedDate,
                coverImageUrl = book.CoverImageUrl,
                stockQuantity = book.StockQuantity,
                availabilityStatus,
                culture = cultureCode,
                availableCultures
            };

            return Results.Ok(result);
        })
        .WithName("GetLocalizedBook")
        .WithDescription("Get a book with localized metadata (title, category, availability) in specified culture")
        .WithTags("Books", "Localization")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/books/{id:guid}/localizations",
            async (Guid id, [FromBody] CreateBookLocalizationRequest request,
                [FromServices] IBookLocalizationService localizationService) =>
        {
            try
            {
                var localization = await localizationService.CreateOrUpdateLocalizationAsync(
                    id, request.CultureCode, request.LocalizedTitle, request.LocalizedDescription);

                return Results.Ok(new BookLocalizationDto(
                    localization.CultureCode,
                    localization.LocalizedTitle,
                    localization.LocalizedDescription
                ));
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("CreateBookLocalization")
        .WithDescription("Create or update a localized version of a book title and description")
        .WithTags("Books", "Localization")
        .Produces<BookLocalizationDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/books/{id:guid}/localizations",
            async (Guid id, [FromServices] IBookLocalizationService localizationService) =>
        {
            var localizations = await localizationService.GetAllLocalizationsAsync(id);
            var result = localizations.Select(l => new BookLocalizationDto(
                l.CultureCode,
                l.LocalizedTitle,
                l.LocalizedDescription
            ));

            return Results.Ok(result);
        })
        .WithName("GetBookLocalizations")
        .WithDescription("Get all available localizations for a specific book")
        .WithTags("Books", "Localization")
        .Produces<IEnumerable<BookLocalizationDto>>(StatusCodes.Status200OK);

        app.MapDelete("/books/{id:guid}/localizations/{culture}",
            async (Guid id, string culture, [FromServices] IBookLocalizationService localizationService) =>
        {
            var deleted = await localizationService.DeleteLocalizationAsync(id, culture);

            if (!deleted)
                return Results.NotFound(new { message = $"Localization not found for book {id} in culture {culture}" });

            return Results.NoContent();
        })
        .WithName("DeleteBookLocalization")
        .WithDescription("Delete a specific localization for a book")
        .WithTags("Books", "Localization")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static void MapMetadataEndpoints(this WebApplication app)
    {
        app.MapGet("/metadata/categories",
            ([FromQuery] string? culture, [FromServices] IBookMetadataService metadataService) =>
        {
            var cultureCode = culture ?? "en-US";
            var categories = metadataService.GetAllCategoryNames(cultureCode);

            return Results.Ok(new
            {
                culture = cultureCode,
                supported = metadataService.IsCultureSupported(cultureCode),
                categories
            });
        })
        .WithName("GetLocalizedCategories")
        .WithDescription("Get all category names localized in the specified culture")
        .WithTags("Metadata", "Localization")
        .Produces(StatusCodes.Status200OK);
    }

    private static void MapCacheEndpoints(this WebApplication app)
    {
        app.MapGet("/books/cache/stats", ([FromServices] IBookCacheService cacheService) =>
        {
            var stats = cacheService.GetCacheStats();
            return Results.Ok(stats);
        })
        .WithName("GetCacheStats")
        .WithDescription("Get cache performance statistics for monitoring")
        .WithTags("Cache")
        .Produces(StatusCodes.Status200OK);
    }

    private static void MapMetricsEndpoints(this WebApplication app)
    {
        app.MapGet("/books/metrics", async ([FromServices] GetBookMetricsHandler handler) =>
        {
            return await handler.Handle();
        })
        .WithName("GetBookMetrics")
        .WithDescription("Get comprehensive book inventory metrics and analytics dashboard")
        .WithTags("Metrics")
        .Produces<BookMetricsDto>(StatusCodes.Status200OK);

        app.MapGet("/books/metrics/performance", async ([FromServices] GetBookMetricsHandler handler) =>
        {
            return await handler.HandlePerformanceMetrics();
        })
        .WithName("GetBookPerformanceMetrics")
        .WithDescription("Get real-time book performance metrics and trends")
        .WithTags("Metrics")
        .Produces<BookPerformanceMetrics>(StatusCodes.Status200OK);
    }
}

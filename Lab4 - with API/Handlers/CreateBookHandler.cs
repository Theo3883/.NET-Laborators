using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Logging;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Handlers;

public class CreateBookHandler(
    BookContext context,
    IValidator<CreateBookProfileRequest> validator,
    IMapper mapper,
    IMemoryCache cache,
    IBookCacheService cacheService,
    ILogger<CreateBookHandler> logger)
{
    public async Task<IResult> Handle(CreateBookProfileRequest request)
    {
        // Generate unique operation ID and start timing
        var operationId = Guid.NewGuid().ToString()[..8];
        var operationStartTime = Stopwatch.GetTimestamp();
        var validationDuration = TimeSpan.Zero;
        var databaseSaveDuration = TimeSpan.Zero;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["OperationType"] = "BookCreation"
        });

        logger.LogInformation(
            LogEvents.BookCreationStarted,
            "Book creation operation started - OperationId: {OperationId}, Title: {Title}, Author: {Author}, ISBN: {ISBN}, Category: {Category}",
            operationId, request.Title, request.Author, request.ISBN, request.Category);

        try
        {
            // === VALIDATION PHASE ===
            var validationStartTime = Stopwatch.GetTimestamp();

            // FluentValidation
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                validationDuration = Stopwatch.GetElapsedTime(validationStartTime);

                logger.LogWarning(
                    LogEvents.BookValidationFailed,
                    "Validation failed for book creation - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}, Errors: {Errors}",
                    operationId, request.Title, request.ISBN,
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
                var metrics = new BookCreationMetrics(
                    operationId, request.Title, request.ISBN, request.Category,
                    validationDuration, TimeSpan.Zero, totalDuration, false, "Validation failed");
                logger.LogBookCreationMetrics(metrics);

                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // ISBN uniqueness validation
            logger.LogDebug(
                LogEvents.ISBNValidationPerformed,
                "Performing ISBN uniqueness check - OperationId: {OperationId}, ISBN: {ISBN}",
                operationId, request.ISBN);

            var isbnExists = await context.Books.AnyAsync(b => b.ISBN == request.ISBN);
            if (isbnExists)
            {
                validationDuration = Stopwatch.GetElapsedTime(validationStartTime);

                logger.LogWarning(
                    LogEvents.ISBNValidationPerformed,
                    "ISBN uniqueness validation failed - OperationId: {OperationId}, ISBN: {ISBN}, Exists: true",
                    operationId, request.ISBN);

                var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
                var metrics = new BookCreationMetrics(
                    operationId, request.Title, request.ISBN, request.Category,
                    validationDuration, TimeSpan.Zero, totalDuration, false, $"Duplicate ISBN: {request.ISBN}");
                logger.LogBookCreationMetrics(metrics);

                return Results.Conflict(new { error = $"A book with ISBN '{request.ISBN}' already exists." });
            }

            logger.LogDebug(
                LogEvents.ISBNValidationPerformed,
                "ISBN uniqueness validation passed - OperationId: {OperationId}, ISBN: {ISBN}",
                operationId, request.ISBN);

            // Stock quantity validation logging
            logger.LogDebug(
                LogEvents.StockValidationPerformed,
                "Stock validation performed - OperationId: {OperationId}, StockQuantity: {StockQuantity}, Valid: {Valid}",
                operationId, request.StockQuantity, request.StockQuantity >= 0);

            validationDuration = Stopwatch.GetElapsedTime(validationStartTime);

            // === DATABASE OPERATION PHASE ===
            var databaseStartTime = Stopwatch.GetTimestamp();

            logger.LogDebug(
                LogEvents.DatabaseOperationStarted,
                "Database operation started - OperationId: {OperationId}, Title: {Title}, ISBN: {ISBN}",
                operationId, request.Title, request.ISBN);

            // Map request to Book entity using AutoMapper
            var book = mapper.Map<Book>(request);

            // Add to database
            context.Books.Add(book);
            await context.SaveChangesAsync();

            databaseSaveDuration = Stopwatch.GetElapsedTime(databaseStartTime);

            logger.LogInformation(
                LogEvents.DatabaseOperationCompleted,
                "Database operation completed - OperationId: {OperationId}, BookId: {BookId}, Title: {Title}, ISBN: {ISBN}, Duration: {DurationMs}ms",
                operationId, book.Id, book.Title, book.ISBN, databaseSaveDuration.TotalMilliseconds);

            // Map to DTO for response
            var bookProfileDto = mapper.Map<BookProfileDto>(book);

            // === CACHE OPERATION - Category-specific invalidation ===
            cacheService.InvalidateCategoryCache(book.Category);
            cache.Remove("all_books"); // Also invalidate general cache
            
            logger.LogInformation(
                LogEvents.CacheOperationPerformed,
                "Category-specific cache invalidation performed - OperationId: {OperationId}, Category: {Category}",
                operationId, book.Category);

            // === LOG SUCCESS METRICS ===
            var totalOperationDuration = Stopwatch.GetElapsedTime(operationStartTime);
            var successMetrics = new BookCreationMetrics(
                operationId, book.Title, book.ISBN, book.Category,
                validationDuration, databaseSaveDuration, totalOperationDuration, true);
            logger.LogBookCreationMetrics(successMetrics);

            return Results.Created($"/books/{book.Id}", bookProfileDto);
        }
        catch (Exception ex)
        {
            var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
            var errorMetrics = new BookCreationMetrics(
                operationId, request.Title, request.ISBN, request.Category,
                validationDuration, databaseSaveDuration, totalDuration, false, ex.Message);
            logger.LogBookCreationMetrics(errorMetrics);

            logger.LogError(ex,
                "Error creating book - OperationId: {OperationId}, Title: {Title}, Author: {Author}, ISBN: {ISBN}",
                operationId, request.Title, request.Author, request.ISBN);

            // Re-throw for global exception handler
            throw;
        }
    }
}
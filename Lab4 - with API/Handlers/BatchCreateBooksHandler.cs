using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Handlers;


public class BatchCreateBooksHandler(
    IServiceProvider serviceProvider,
    IMapper mapper,
    IMemoryCache cache,
    IBookCacheService cacheService,
    ILogger<BatchCreateBooksHandler> logger)
{
    private const int MaxBatchSize = 100;
    private const int ParallelValidationThreshold = 10;

    public async Task<IResult> Handle(BatchCreateBooksRequest request)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var operationStartTime = Stopwatch.GetTimestamp();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["OperationType"] = "BatchBookCreation",
            ["BatchSize"] = request.Books.Count
        });

        logger.LogInformation(
            "Batch book creation started - OperationId: {OperationId}, BatchSize: {BatchSize}",
            operationId, request.Books.Count);

        // Validate batch size
        if (request.Books.Count == 0)
        {
            logger.LogWarning("Empty batch request - OperationId: {OperationId}", operationId);
            return Results.BadRequest(new { error = "Batch request cannot be empty" });
        }

        if (request.Books.Count > MaxBatchSize)
        {
            logger.LogWarning(
                "Batch size exceeds limit - OperationId: {OperationId}, Size: {Size}, Max: {Max}",
                operationId, request.Books.Count, MaxBatchSize);
            return Results.BadRequest(new { error = $"Batch size cannot exceed {MaxBatchSize} books" });
        }

        try
        {
            // Phase 1: Parallel Validation
            logger.LogDebug("Starting parallel validation phase - OperationId: {OperationId}", operationId);
            var validationStartTime = Stopwatch.GetTimestamp();

            var validationResults = await ValidateBooksInParallel(request.Books);
            var validationDuration = Stopwatch.GetElapsedTime(validationStartTime);

            logger.LogInformation(
                "Validation completed - OperationId: {OperationId}, Valid: {Valid}, Invalid: {Invalid}, Duration: {DurationMs}ms",
                operationId, validationResults.ValidBooks.Count, validationResults.Errors.Count, validationDuration.TotalMilliseconds);

            // If all books are invalid, return errors
            if (validationResults.ValidBooks.Count == 0)
            {
                var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
                return Results.BadRequest(new BatchCreateBooksResponse(
                    TotalRequested: request.Books.Count,
                    SuccessfullyCreated: 0,
                    Failed: request.Books.Count,
                    CreatedBooks: new List<BookProfileDto>(),
                    Errors: validationResults.Errors,
                    ProcessingTime: totalDuration,
                    OperationId: operationId
                ));
            }

            // Phase 2: Database Transaction with Batch Insert
            logger.LogDebug(
                "Starting database transaction - OperationId: {OperationId}, BooksToCreate: {Count}",
                operationId, validationResults.ValidBooks.Count);

            var databaseStartTime = Stopwatch.GetTimestamp();
            List<Book> createdBooks;

            // Create a scope for database operations
            using (var dbScope = serviceProvider.CreateScope())
            {
                var context = dbScope.ServiceProvider.GetRequiredService<BookContext>();

                await using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Check for duplicate ISBNs in database
                        var isbnsToCheck = validationResults.ValidBooks
                            .Select(b => b.Request.ISBN)
                            .ToList();

                        var existingIsbns = await context.Books
                            .Where(b => isbnsToCheck.Contains(b.ISBN))
                            .Select(b => b.ISBN)
                            .ToHashSetAsync();

                        // Filter out duplicates
                        var booksToCreate = validationResults.ValidBooks
                            .Where(b => !existingIsbns.Contains(b.Request.ISBN))
                            .ToList();

                        // Add duplicate errors
                        foreach (var validBook in validationResults.ValidBooks)
                        {
                            if (existingIsbns.Contains(validBook.Request.ISBN))
                            {
                                validationResults.Errors.Add(new BatchBookError(
                                    validBook.Index,
                                    validBook.Request.Title,
                                    validBook.Request.ISBN,
                                    new List<string> { $"A book with ISBN '{validBook.Request.ISBN}' already exists." }
                                ));
                            }
                        }

                        if (booksToCreate.Count == 0)
                        {
                            await transaction.RollbackAsync();
                            var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
                            
                            logger.LogWarning(
                                "All books failed validation or duplicates - OperationId: {OperationId}",
                                operationId);

                            return Results.BadRequest(new BatchCreateBooksResponse(
                                TotalRequested: request.Books.Count,
                                SuccessfullyCreated: 0,
                                Failed: request.Books.Count,
                                CreatedBooks: new List<BookProfileDto>(),
                                Errors: validationResults.Errors,
                                ProcessingTime: totalDuration,
                                OperationId: operationId
                            ));
                        }

                        // Map to entities
                        createdBooks = booksToCreate
                            .Select(vb => mapper.Map<Book>(vb.Request))
                            .ToList();

                        // Batch insert
                        await context.Books.AddRangeAsync(createdBooks);
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var databaseDuration = Stopwatch.GetElapsedTime(databaseStartTime);
                        logger.LogInformation(
                            "Batch insert completed - OperationId: {OperationId}, BooksCreated: {Count}, Duration: {DurationMs}ms",
                            operationId, createdBooks.Count, databaseDuration.TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        logger.LogError(ex,
                            "Transaction failed - OperationId: {OperationId}",
                            operationId);
                        throw;
                    }
                }
            }

            // Phase 3: Parallel Mapping to DTOs
            logger.LogDebug("Starting parallel DTO mapping - OperationId: {OperationId}", operationId);
            var mappingStartTime = Stopwatch.GetTimestamp();

            var bookDtos = await MapBooksToDtosInParallel(createdBooks);
            
            var mappingDuration = Stopwatch.GetElapsedTime(mappingStartTime);
            logger.LogDebug(
                "DTO mapping completed - OperationId: {OperationId}, Duration: {DurationMs}ms",
                operationId, mappingDuration.TotalMilliseconds);

            // Phase 4: Cache Invalidation
            var affectedCategories = createdBooks
                .Select(b => b.Category)
                .Distinct()
                .ToList();

            foreach (var category in affectedCategories)
            {
                cacheService.InvalidateCategoryCache(category);
            }
            cache.Remove("all_books");

            logger.LogInformation(
                "Cache invalidated for categories - OperationId: {OperationId}, Categories: {Categories}",
                operationId, string.Join(", ", affectedCategories));

            // Prepare response
            var totalOperationDuration = Stopwatch.GetElapsedTime(operationStartTime);
            var response = new BatchCreateBooksResponse(
                TotalRequested: request.Books.Count,
                SuccessfullyCreated: createdBooks.Count,
                Failed: validationResults.Errors.Count,
                CreatedBooks: bookDtos,
                Errors: validationResults.Errors,
                ProcessingTime: totalOperationDuration,
                OperationId: operationId
            );

            logger.LogInformation(
                "Batch creation completed - OperationId: {OperationId}, Success: {Success}, Failed: {Failed}, TotalDuration: {DurationMs}ms",
                operationId, createdBooks.Count, validationResults.Errors.Count, totalOperationDuration.TotalMilliseconds);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);
            logger.LogError(ex,
                "Batch book creation failed - OperationId: {OperationId}, Duration: {DurationMs}ms",
                operationId, totalDuration.TotalMilliseconds);
            throw;
        }
    }

    private async Task<ValidationResults> ValidateBooksInParallel(List<CreateBookProfileRequest> books)
    {
        var validBooks = new List<ValidatedBook>();
        var errors = new List<BatchBookError>();
        var lockObj = new object();

        if (books.Count >= ParallelValidationThreshold)
        {
            // Use parallel validation for large batches
            logger.LogDebug("Using parallel validation for {Count} books", books.Count);
            
            await Parallel.ForEachAsync(
                books.Select((book, index) => (book, index)),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (item, cancellationToken) =>
                {
                    // Create a new scope for each parallel validation to ensure thread safety
                    using var validationScope = serviceProvider.CreateScope();
                    var validator = validationScope.ServiceProvider.GetRequiredService<IValidator<CreateBookProfileRequest>>();
                    
                    var validationResult = await validator.ValidateAsync(item.book, cancellationToken);
                    
                    lock (lockObj)
                    {
                        if (validationResult.IsValid)
                        {
                            validBooks.Add(new ValidatedBook(item.index, item.book));
                        }
                        else
                        {
                            errors.Add(new BatchBookError(
                                item.index,
                                item.book.Title,
                                item.book.ISBN,
                                validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                            ));
                        }
                    }
                });
        }
        else
        {
            // Sequential validation for small batches
            logger.LogDebug("Using sequential validation for {Count} books", books.Count);
            
            using var validationScope = serviceProvider.CreateScope();
            var validator = validationScope.ServiceProvider.GetRequiredService<IValidator<CreateBookProfileRequest>>();
            
            for (int i = 0; i < books.Count; i++)
            {
                var validationResult = await validator.ValidateAsync(books[i]);
                
                if (validationResult.IsValid)
                {
                    validBooks.Add(new ValidatedBook(i, books[i]));
                }
                else
                {
                    errors.Add(new BatchBookError(
                        i,
                        books[i].Title,
                        books[i].ISBN,
                        validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                    ));
                }
            }
        }

        return new ValidationResults(validBooks, errors);
    }

    private async Task<List<BookProfileDto>> MapBooksToDtosInParallel(List<Book> books)
    {
        if (books.Count >= ParallelValidationThreshold)
        {
            // Parallel mapping for large batches
            var tasks = books.Select(book => Task.Run(() => mapper.Map<BookProfileDto>(book)));
            return (await Task.WhenAll(tasks)).ToList();
        }
        else
        {
            // Sequential mapping for small batches
            return books.Select(book => mapper.Map<BookProfileDto>(book)).ToList();
        }
    }

    private record ValidationResults(
        List<ValidatedBook> ValidBooks,
        List<BatchBookError> Errors
    );

    private record ValidatedBook(int Index, CreateBookProfileRequest Request);
}

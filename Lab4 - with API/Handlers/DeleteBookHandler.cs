using FluentValidation;
using Lab3.Persistence;
using Lab3.DTO.Request;
using Lab3.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Handlers;

public class DeleteBookHandler(
    BookContext context,
    IValidator<DeleteBookRequest> validator,
    IMemoryCache cache,
    IBookCacheService cacheService,
    ILogger<DeleteBookHandler> logger)
{
    public async Task<IResult> Handle(DeleteBookRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for DeleteBookRequest - ID: {Id}", request.Id);
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = await context.Books.FindAsync(request.Id);
        if (book == null)
        {
            logger.LogWarning("Book not found for deletion - ID: {Id}", request.Id);
            return Results.NotFound(new { error = $"Book with ID '{request.Id}' not found." });
        }

        var bookCategory = book.Category;
        
        context.Books.Remove(book);
        await context.SaveChangesAsync();

        logger.LogInformation("Book deleted successfully - ID: {Id}, Title: {Title}, Category: {Category}", 
            book.Id, book.Title, bookCategory);

        // Category-specific cache invalidation
        cacheService.InvalidateCategoryCache(bookCategory);
        cache.Remove("all_books");
        logger.LogDebug("Cache invalidated for category: {Category}", bookCategory);

        return Results.NoContent();
    }
}
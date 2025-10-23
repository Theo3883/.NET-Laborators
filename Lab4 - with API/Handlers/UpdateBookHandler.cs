using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Handlers;

public class UpdateBookHandler(
    BookContext context,
    IValidator<UpdateBookRequest> validator,
    IMapper mapper,
    IMemoryCache cache,
    IBookCacheService cacheService,
    ILogger<UpdateBookHandler> logger)
{
    public async Task<IResult> Handle(UpdateBookRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for UpdateBookRequest - ID: {Id}, Errors: {Errors}",
                request.Id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var existingBook = await context.Books.FindAsync(request.Id);
        if (existingBook == null)
        {
            logger.LogWarning("Book not found for update - ID: {Id}", request.Id);
            return Results.NotFound(new { error = $"Book with ID '{request.Id}' not found." });
        }

        // Store old category for cache invalidation
        var oldCategory = existingBook.Category;

        // Update properties
        existingBook.Title = request.Title;
        existingBook.Author = request.Author;
        existingBook.ISBN = request.ISBN;
        existingBook.Category = request.Category;
        existingBook.Price = request.Price;
        existingBook.PublishedDate = request.PublishedDate;
        existingBook.CoverImageUrl = request.CoverImageUrl;
        existingBook.StockQuantity = request.StockQuantity;

        await context.SaveChangesAsync();

        logger.LogInformation("Book updated successfully - ID: {Id}, Title: {Title}", existingBook.Id, existingBook.Title);

        // Map to DTO
        var bookProfileDto = mapper.Map<BookProfileDto>(existingBook);

        // Category-specific cache invalidation
        cacheService.InvalidateCategoryCache(oldCategory);
        if (oldCategory != request.Category)
        {
            // If category changed, invalidate both old and new category caches
            cacheService.InvalidateCategoryCache(request.Category);
            logger.LogInformation("Category changed - invalidating both caches - OldCategory: {OldCategory}, NewCategory: {NewCategory}",
                oldCategory, request.Category);
        }
        
        cache.Remove("all_books");
        logger.LogDebug("Cache invalidated for category: {Category}", existingBook.Category);

        return Results.Ok(bookProfileDto);
    }
}

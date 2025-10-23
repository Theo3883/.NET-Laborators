using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;
using Lab3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lab3.Handlers;

public class GetBooksWithPaginationHandler(
    BookContext context,
    IValidator<GetBooksWithPaginationRequest> validator,
    IMapper mapper,
    IMemoryCache cache,
    IBookCacheService cacheService,
    ILogger<GetBooksWithPaginationHandler> logger)
{
    public async Task<IResult> Handle(GetBooksWithPaginationRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for GetBooksWithPaginationRequest - Page: {Page}, PageSize: {PageSize}, Category: {Category}",
                request.Page, request.PageSize, request.Category);
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Category-based cache key for better cache hit rates
        var cacheKey = request.Category.HasValue
            ? $"books_{request.Category}_page_{request.Page}_size_{request.PageSize}"
            : $"books_all_page_{request.Page}_size_{request.PageSize}";

        if (cache.TryGetValue(cacheKey, out object? cachedResponse))
        {
            cacheService.RecordCacheHit();
            logger.LogDebug("Cache HIT - Page: {Page}, PageSize: {PageSize}, Category: {Category}, CacheKey: {CacheKey}", 
                request.Page, request.PageSize, request.Category, cacheKey);
            return Results.Ok(cachedResponse);
        }

        cacheService.RecordCacheMiss();
        logger.LogDebug("Cache MISS - Querying database - Page: {Page}, PageSize: {PageSize}, Category: {Category}", 
            request.Page, request.PageSize, request.Category);

        // Build query with optional category filter
        var query = context.Books.AsNoTracking();
        
        if (request.Category.HasValue)
        {
            query = query.Where(b => b.Category == request.Category.Value);
            logger.LogDebug("Filtering by category: {Category}", request.Category);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var bookDtos = mapper.Map<List<BookProfileDto>>(books);

        var response = new
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Category = request.Category,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.Page > 1,
            HasNextPage = request.Page < totalPages,
            Data = bookDtos
        };

        cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        cacheService.RegisterCacheKey(cacheKey);
        
        logger.LogInformation("Books retrieved and cached - Page: {Page}, PageSize: {PageSize}, Category: {Category}, Count: {Count}, CacheKey: {CacheKey}",
            request.Page, request.PageSize, request.Category, bookDtos.Count, cacheKey);

        return Results.Ok(response);
    }
}

using FluentValidation;
using Lab3.DTO.Request;
 
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;

public class GetBooksWithPaginationHandler(BookContext context, IValidator<GetBooksWithPaginationRequest> validator)
{
    public async Task<IResult> Handle(GetBooksWithPaginationRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var totalCount = await context.Books.CountAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var books = await context.Books
            .AsNoTracking()
            .OrderBy(b => b.Id) 
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var response = new
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.Page > 1,
            HasNextPage = request.Page < totalPages,
            Data = books
        };

        return Results.Ok(response);
    }
}

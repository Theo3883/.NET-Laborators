using Lab3.DTO.Request;
 
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Handlers;

public class GetAllBooksHandler(BookContext context)
{
    public async Task<IResult> Handle(GetAllBooksRequest request)
    {
        var books = await context.Books.ToListAsync();
        return Results.Ok(books);
    }
}
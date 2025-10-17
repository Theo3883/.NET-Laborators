using Lab3.DTO.Request;
using Lab3.Persistence;

namespace Lab3.Handlers;

public class GetBookByIdHandler(BookContext context)
{
    public async Task<IResult> Handle(GetBookByIdRequest request)
    {
        var book =  await context.Books.FindAsync(request.Id);
        if (book == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(book);
    }
 
    
}
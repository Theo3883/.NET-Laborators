using FluentValidation;
using Lab3.Persistence;
using Lab3.DTO.Request;

namespace Lab3.Handlers;

public class DeleteBookHandler(BookContext context, IValidator<DeleteBookRequest> validator)
{
    public async Task<IResult> Handle(DeleteBookRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = await context.Books.FindAsync(request.Id);
        if (book == null)
        {
            return Results.NotFound();
        }
        context.Books.Remove(book);
        await context.SaveChangesAsync();
        return Results.NoContent();
    }
    
}
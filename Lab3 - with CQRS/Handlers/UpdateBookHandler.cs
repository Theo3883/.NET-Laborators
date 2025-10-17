using FluentValidation;
using Lab3.DTO.Request;
 
using Lab3.Model;
using Lab3.Persistence;

namespace Lab3.Handlers;

public class UpdateBookHandler(BookContext context, IValidator<UpdateBookRequest> validator)
{
    public async Task<IResult> Handle(UpdateBookRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var existingBook = await context.Books.FindAsync(request.Id);
        if (existingBook == null)
        {
            return Results.NotFound();
        }

        var updatedBook = new Book(request.Id, request.Title, request.Author, request.Year);
        context.Books.Remove(existingBook);
        context.Books.Add(updatedBook);
        await context.SaveChangesAsync();

        return Results.Ok(updatedBook);
    }
}

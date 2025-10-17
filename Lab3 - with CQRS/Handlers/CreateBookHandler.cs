using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;

namespace Lab3.Handlers;

public class CreateBookHandler(BookContext context, IValidator<CreateBookRequest> validator)
{
    public async Task<IResult> Handle(CreateBookRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = new Book(Random.Shared.Next(1, int.MaxValue), request.Title, request.Author, request.Year);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        return Results.Created($"/books/{book.Id}", book);
    }
    
}
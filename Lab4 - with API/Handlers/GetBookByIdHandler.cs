using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Persistence;

namespace Lab3.Handlers;

public class GetBookByIdHandler(
    BookContext context,
    IValidator<GetBookByIdRequest> validator,
    IMapper mapper,
    ILogger<GetBookByIdHandler> logger)
{
    public async Task<IResult> Handle(GetBookByIdRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for GetBookByIdRequest - ID: {Id}", request.Id);
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = await context.Books.FindAsync(request.Id);
        if (book == null)
        {
            logger.LogDebug("Book not found - ID: {Id}", request.Id);
            return Results.NotFound(new { error = $"Book with ID '{request.Id}' not found." });
        }

        logger.LogDebug("Book retrieved - ID: {Id}, Title: {Title}", book.Id, book.Title);

        var bookProfileDto = mapper.Map<BookProfileDto>(book);
        return Results.Ok(bookProfileDto);
    }
}
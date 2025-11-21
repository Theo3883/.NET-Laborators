using FluentValidation;
using WebApplication1.DTO.Request;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApplication1.Handlers;

public class CreatePaperHandler
{
    private readonly PaperContext _context;
    private readonly IValidator<CreatePaperRequest> _validator;
    private readonly ILogger<CreatePaperHandler> _logger;

    public CreatePaperHandler(
        PaperContext context,
        IValidator<CreatePaperRequest> validator,
        ILogger<CreatePaperHandler> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Results<Created<Paper>, ValidationProblem>> Handle(
        CreatePaperRequest request, 
        HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation("Creating paper - Title: {Title}, Author: {Author}, TraceId: {TraceId}", 
            request.Title, request.Author, traceId);

        // Validation phase
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            };

            return TypedResults.ValidationProblem(errors, extensions: extensions);
        }

        // Create paper entity
        var paper = new Paper
        {
            Title = request.Title,
            Author = request.Author,
            PublishedOn = request.PublishedOn
        };
        
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Paper created successfully - Id: {Id}, Title: {Title}", paper.Id, paper.Title);

        return TypedResults.Created($"/papers/{paper.Id}", paper);
    }
}


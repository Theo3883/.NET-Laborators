using FluentValidation;
using WebApplication1.DTO.Request;
using WebApplication1.Mappers;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApplication1.Handlers;

/// <summary>
/// Handler for creating new papers
/// </summary>
public class CreatePaperHandler(
    PaperContext context,
    IValidator<CreatePaperRequest> validator,
    IPaperMapper mapper,
    ILogger<CreatePaperHandler> logger)
    : ICreatePaperHandler
{
    private readonly PaperContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IValidator<CreatePaperRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IPaperMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly ILogger<CreatePaperHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        // Map request to entity using mapper abstraction
        var paper = _mapper.MapToEntity(request);
        
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Paper created successfully - Id: {Id}, Title: {Title}", paper.Id, paper.Title);

        return TypedResults.Created($"/papers/{paper.Id}", paper);
    }
}


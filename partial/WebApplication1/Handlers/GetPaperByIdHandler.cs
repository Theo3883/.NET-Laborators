using WebApplication1.DTO.Request;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

/// <summary>
/// Handler for retrieving a paper by ID
/// </summary>
public class GetPaperByIdHandler(
    PaperContext context,
    ILogger<GetPaperByIdHandler> logger)
    : IGetPaperByIdHandler
{
    private readonly PaperContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<GetPaperByIdHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Results<Ok<Paper>, NotFound>> Handle(
        GetPaperByIdRequest request, 
        HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation("Getting paper by ID - Id: {Id}, TraceId: {TraceId}", request.Id, traceId);

        var paper = await _context.Papers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id);

        if (paper == null)
        {
            _logger.LogWarning("Paper not found - Id: {Id}, TraceId: {TraceId}", request.Id, traceId);
            return TypedResults.NotFound();
        }

        _logger.LogInformation("Paper retrieved successfully - Id: {Id}, Title: {Title}", paper.Id, paper.Title);
        return TypedResults.Ok(paper);
    }
}


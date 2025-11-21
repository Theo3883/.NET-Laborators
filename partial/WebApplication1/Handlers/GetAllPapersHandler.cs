using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

/// <summary>
/// Handler for retrieving all papers
/// </summary>
public class GetAllPapersHandler(
    PaperContext context,
    ILogger<GetAllPapersHandler> logger)
    : IGetAllPapersHandler
{
    private readonly PaperContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<GetAllPapersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Ok<List<Paper>>> Handle(HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation("Getting all papers - TraceId: {TraceId}", traceId);

        var papers = await _context.Papers
            .AsNoTracking()
            .OrderByDescending(p => p.PublishedOn)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} papers", papers.Count);
        return TypedResults.Ok(papers);
    }
}


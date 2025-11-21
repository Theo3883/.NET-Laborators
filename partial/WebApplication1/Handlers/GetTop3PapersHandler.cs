using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

/// <summary>
/// Handler for retrieving top 3 most recent papers
/// </summary>
public class GetTop3PapersHandler(
    PaperContext context,
    ILogger<GetTop3PapersHandler> logger)
    : IGetTop3PapersHandler
{
    private readonly PaperContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<GetTop3PapersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Ok<List<Paper>>> Handle(HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        
        _logger.LogInformation("Getting top 3 most recent papers - TraceId: {TraceId}", traceId);

        var papers = await _context.Papers
            .AsNoTracking()
            .OrderByDescending(p => p.PublishedOn)
            .Take(3)
            .ToListAsync();

        _logger.LogInformation("Retrieved top {Count} papers", papers.Count);
        return TypedResults.Ok(papers);
    }
}


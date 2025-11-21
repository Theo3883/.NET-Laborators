using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

public class GetTop3PapersHandler
{
    private readonly PaperContext _context;
    private readonly ILogger<GetTop3PapersHandler> _logger;

    public GetTop3PapersHandler(
        PaperContext context,
        ILogger<GetTop3PapersHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

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


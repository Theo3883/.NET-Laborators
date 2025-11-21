using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

public class GetAllPapersHandler
{
    private readonly PaperContext _context;
    private readonly ILogger<GetAllPapersHandler> _logger;

    public GetAllPapersHandler(
        PaperContext context,
        ILogger<GetAllPapersHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

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


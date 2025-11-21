using WebApplication1.DTO.Request;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Handlers;

public class GetPaperByIdHandler
{
    private readonly PaperContext _context;
    private readonly ILogger<GetPaperByIdHandler> _logger;

    public GetPaperByIdHandler(
        PaperContext context,
        ILogger<GetPaperByIdHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

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


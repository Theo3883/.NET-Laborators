using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.Model;

namespace WebApplication1.Handlers;

/// <summary>
/// Interface for retrieving top 3 most recent papers
/// </summary>
public interface IGetTop3PapersHandler
{
    Task<Ok<List<Paper>>> Handle(HttpContext httpContext);
}


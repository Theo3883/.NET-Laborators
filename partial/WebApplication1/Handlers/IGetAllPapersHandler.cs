using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.Model;

namespace WebApplication1.Handlers;

/// <summary>
/// Interface for retrieving all papers
/// </summary>
public interface IGetAllPapersHandler
{
    Task<Ok<List<Paper>>> Handle(HttpContext httpContext);
}


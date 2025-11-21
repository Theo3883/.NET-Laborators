using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.DTO.Request;
using WebApplication1.Model;

namespace WebApplication1.Handlers;

/// <summary>
/// Interface for creating papers
/// </summary>
public interface ICreatePaperHandler
{
    Task<Results<Created<Paper>, ValidationProblem>> Handle(CreatePaperRequest request, HttpContext httpContext);
}


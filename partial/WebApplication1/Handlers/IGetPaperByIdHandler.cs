using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.DTO.Request;
using WebApplication1.Model;

namespace WebApplication1.Handlers;

/// <summary>
/// Interface for retrieving a paper by ID
/// </summary>
public interface IGetPaperByIdHandler
{
    Task<Results<Ok<Paper>, NotFound>> Handle(GetPaperByIdRequest request, HttpContext httpContext);
}


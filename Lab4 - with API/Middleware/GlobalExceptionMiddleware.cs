using System.Net;
using System.Text.Json;

namespace Lab3.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ErrorResponse errorResponse;
        int statusCode;

        // For now, handle all exceptions as internal server errors
        // Can be extended with custom exception types later
        errorResponse = new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred")
        {
            TraceId = context.TraceIdentifier
        };
        statusCode = (int)HttpStatusCode.InternalServerError;

        context.Response.StatusCode = statusCode;
        var response = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(response);
    }
}

public class ErrorResponse()
{
    public ErrorResponse(string errorCode, string message) : this()
    {
        ErrorCode = errorCode;
        Message = message;
    }
    
    public ErrorResponse(string errorCode, string message, List<string> details) : this(errorCode, message)
    {
        Details = details;
    }

    public List<string>? Details { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;
    
    public string TraceId { get; set; } = string.Empty;
}

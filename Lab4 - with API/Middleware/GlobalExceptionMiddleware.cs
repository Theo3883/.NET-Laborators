using System.Net;
using System.Text.Json;

namespace Lab3.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to logging scope
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            logger.LogDebug("Request started - CorrelationId: {CorrelationId}, Method: {Method}, Path: {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            try
            {
                await next(context);

                logger.LogDebug("Request completed - CorrelationId: {CorrelationId}, StatusCode: {StatusCode}",
                    correlationId, context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                    correlationId, context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ErrorResponse errorResponse;
        int statusCode;

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

namespace WebApplication1.Configuration;

/// <summary>
/// Configures middleware pipeline
/// </summary>
public static class MiddlewareConfiguration
{
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Development-specific middleware
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        return app;
    }
}


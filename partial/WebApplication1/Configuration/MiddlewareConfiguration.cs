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
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Papers API V1");
                options.RoutePrefix = string.Empty; // Swagger UI at app's root
                options.DisplayRequestDuration();
            });
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        return app;
    }
}


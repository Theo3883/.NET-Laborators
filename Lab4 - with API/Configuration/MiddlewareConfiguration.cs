using Lab3.Middleware;

namespace Lab3.Configuration;


/// Configures middleware pipeline 
public static class MiddlewareConfiguration
{
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Global exception handling with TraceId correlation
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // Development-specific middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API V1");
                options.RoutePrefix = string.Empty; // Swagger UI at app's root
                options.DisplayRequestDuration();
            });
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        return app;
    }
}

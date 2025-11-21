using FluentValidation;
using WebApplication1.Persistence;
using WebApplication1.Handlers;
using WebApplication1.Mappers;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Configuration;

/// <summary>
/// Configures dependency injection services
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // FluentValidation - register all validators from assembly
        services.AddValidatorsFromAssemblyContaining(typeof(Program), ServiceLifetime.Scoped);

        // Database
        services.AddDbContext<PaperContext>(options => 
            options.UseSqlite("Data Source=papers.db"));

        // Mappers - Register mapper abstraction with concrete implementation
        services.AddSingleton<IPaperMapper, PaperMapper>();

        // Paper Handlers - Register interface with implementation
        services.AddScoped<ICreatePaperHandler, CreatePaperHandler>();
        services.AddScoped<IGetPaperByIdHandler, GetPaperByIdHandler>();
        services.AddScoped<IGetAllPapersHandler, GetAllPapersHandler>();
        services.AddScoped<IGetTop3PapersHandler, GetTop3PapersHandler>();
        
        // Also register concrete types for backward compatibility with endpoints
        services.AddScoped<CreatePaperHandler>();
        services.AddScoped<GetPaperByIdHandler>();
        services.AddScoped<GetAllPapersHandler>();
        services.AddScoped<GetTop3PapersHandler>();

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        
        // Add Swagger/OpenAPI support
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Papers API",
                Version = "v1",
                Description = "API for managing academic papers"
            });
        });

        return services;
    }

    public static void EnsureDatabaseCreated(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaperContext>();
        db.Database.EnsureCreated();
        
        // Seed initial data
        db.SeedData();
    }
}


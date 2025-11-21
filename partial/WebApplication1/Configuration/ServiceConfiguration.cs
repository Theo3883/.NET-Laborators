using FluentValidation;
using WebApplication1.Persistence;
using WebApplication1.Handlers;
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

        // Paper Handlers
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


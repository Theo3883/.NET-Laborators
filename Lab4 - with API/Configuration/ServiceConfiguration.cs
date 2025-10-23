using FluentValidation;
using Lab3.Handlers;
using Lab3.Mapping.Resolvers;
using Lab3.Persistence;
using Lab3.Services;
using Lab3.Validators;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Lab3.Configuration;


/// Configures dependency injection services 
public static class ServiceConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Configure JSON serialization
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // AutoMapper with custom resolvers
        services.AddAutoMapper(typeof(Program).Assembly);
        
        // Register AutoMapper custom resolvers
        services.AddTransient<CategoryDisplayResolver>();
        services.AddTransient<PriceFormatterResolver>();
        services.AddTransient<PublishedAgeResolver>();
        services.AddTransient<AuthorInitialsResolver>();
        services.AddTransient<AvailabilityStatusResolver>();
        services.AddTransient<ConditionalCoverImageResolver>();
        services.AddTransient<ConditionalPriceResolver>();

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<IBookCacheService, BookCacheService>();

        // Multi-language support
        services.AddSingleton<IBookMetadataService, BookMetadataService>();
        services.AddScoped<IBookLocalizationService, BookLocalizationService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateBookProfileValidator>(ServiceLifetime.Scoped);

        // Database
        services.AddDbContext<BookContext>(options => 
            options.UseSqlite("Data Source=books.db"));

        // Handlers
        services.AddScoped<CreateBookHandler>();
        services.AddScoped<UpdateBookHandler>();
        services.AddScoped<DeleteBookHandler>();
        services.AddScoped<GetBooksWithPaginationHandler>();
        services.AddScoped<GetBookByIdHandler>();
        services.AddScoped<GetBookMetricsHandler>();
        services.AddScoped<BatchCreateBooksHandler>();

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Book Management API",
                Version = "v1",
                Description = "A comprehensive book management system with multi-language support, caching, and batch operations"
            });
        });

        return services;
    }

    public static void EnsureDatabaseCreated(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookContext>();
        db.Database.EnsureCreated();
    }
}

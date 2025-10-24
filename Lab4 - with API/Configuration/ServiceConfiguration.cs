using FluentValidation;
using Lab3.Handlers;
using Lab3.Mapping.Resolvers;
using Lab3.Persistence;
using Lab3.Services;
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
        
        // Register AutoMapper custom resolvers for Order
        services.AddTransient<OrderCategoryDisplayResolver>();
        services.AddTransient<OrderPriceFormatterResolver>();
        services.AddTransient<OrderPublishedAgeResolver>();
        services.AddTransient<OrderAuthorInitialsResolver>();
        services.AddTransient<OrderAvailabilityStatusResolver>();
        services.AddTransient<ConditionalOrderCoverImageResolver>();
        services.AddTransient<ConditionalOrderPriceResolver>();

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<IOrderCacheService, OrderCacheService>();

        // FluentValidation - register all validators from assembly
        services.AddValidatorsFromAssemblyContaining(typeof(Program), ServiceLifetime.Scoped);

        // Database
        services.AddDbContext<BookContext>(options => 
            options.UseSqlite("Data Source=orders.db"));

        // Order Handlers
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetAllOrdersHandler>();
        services.AddScoped<GetOrdersWithPaginationHandler>();
        services.AddScoped<UpdateOrderHandler>();
        services.AddScoped<DeleteOrderHandler>();

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
                Title = "Order Management API",
                Version = "v1",
                Description = "A comprehensive order management system with caching, validation, and CRUD operations"
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

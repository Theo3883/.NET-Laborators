using Lab3.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure services 
builder.Services.AddApplicationServices();
builder.Services.AddApiDocumentation();

var app = builder.Build();

// Ensure database is created
app.Services.EnsureDatabaseCreated();

// Configure middleware pipeline
app.UseApplicationMiddleware();

// Map all API endpoints
app.MapOrderEndpoints();

app.Run();


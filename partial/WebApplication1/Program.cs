using WebApplication1.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure services 
builder.Services.AddApplicationServices();
builder.Services.AddApiDocumentation();

var app = builder.Build();

// Ensure database is created and seeded
app.Services.EnsureDatabaseCreated();

// Configure middleware pipeline
app.UseApplicationMiddleware();

// Map all API endpoints
app.MapPaperEndpoints();

app.Run();

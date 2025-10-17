using Lab3.Validators;
using Lab3.Persistence;
using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<BookContext>(
    options => options.UseSqlite("Data Source=books.db")
);
builder.Services.AddScoped<CreateBookHandler>();
builder.Services.AddScoped<IValidator<CreateBookRequest>, CreateBookValidator>();
builder.Services.AddScoped<UpdateBookHandler>();
builder.Services.AddScoped<IValidator<UpdateBookRequest>, UpdateBookValidator>();
builder.Services.AddScoped<DeleteBookHandler>();
builder.Services.AddScoped<IValidator<DeleteBookRequest>, DeleteBookValidator>();
builder.Services.AddScoped<GetBooksWithPaginationHandler>();
builder.Services.AddScoped<IValidator<GetBooksWithPaginationRequest>, GetBooksWithPaginationValidator>();
builder.Services.AddScoped<GetBookByIdHandler>();
var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/books", async ([FromBody] CreateBookRequest request, [FromServices] CreateBookHandler handler) =>
{
    return await handler.Handle(request);
});
app.MapGet("/books", async ([FromQuery] int page, [FromQuery] int pageSize, [FromServices] GetBooksWithPaginationHandler handler) =>
{
    
    var request = new GetBooksWithPaginationRequest(page, pageSize);
    return await handler.Handle(request);
});
app.MapGet("/books-all", async (BookContext context) =>
{
    var books = await context.Books.ToListAsync();
    return Results.Ok(books);
});
app.UseHttpsRedirection();
app.MapGet("/books/{id:int}", async (int id, [FromServices] GetBookByIdHandler handler) =>
{
    var request = new GetBookByIdRequest(id);
    return await handler.Handle(request);
});
app.MapPut("/books/{id:int}", async (int id, [FromBody] UpdateBookRequest request, [FromServices] UpdateBookHandler handler) =>
{
    if (id != request.Id)
    {
        return Results.BadRequest("ID in URL does not match ID in request body.");
    }
    return await handler.Handle(request);
});
app.MapDelete("/books/{id:int}", async (int id, [FromServices] DeleteBookHandler handler) =>
{
    var request = new DeleteBookRequest(id);
    return await handler.Handle(request);
});

app.Run();


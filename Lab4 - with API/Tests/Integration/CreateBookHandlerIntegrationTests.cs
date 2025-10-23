using AutoMapper;
using FluentValidation;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Handlers;
using Lab3.Logging;
using Lab3.Mapping;
using Lab3.Model;
using Lab3.Persistence;
using Lab3.Services;
using Lab3.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lab3.Tests.Integration;

public class CreateBookHandlerIntegrationTests : IDisposable
{
    private readonly BookContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateBookHandler>> _mockLogger;
    private readonly CreateBookHandler _handler;

    public CreateBookHandlerIntegrationTests()
    {
        // Set up in-memory database with unique name
        var options = new DbContextOptionsBuilder<BookContext>()
            .UseInMemoryDatabase(databaseName: $"BookTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new BookContext(options);

        // Configure AutoMapper with both book profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedBookMappingProfile>();
        });
        var mapper1 = mapperConfig.CreateMapper();

        // Set up memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Mock logger
        _mockLogger = new Mock<ILogger<CreateBookHandler>>();
        
        // Mock cache service
        var mockCacheService = new Mock<IBookCacheService>();

        // Create validator
        var validatorLogger = new Mock<ILogger<CreateBookProfileValidator>>();
        IValidator<CreateBookProfileRequest> validator1 = new CreateBookProfileValidator(_context, validatorLogger.Object);

        // Create handler instance with all dependencies
        _handler = new CreateBookHandler(_context, validator1, mapper1, _cache, mockCacheService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidTechnicalBookRequest_CreatesBookWithCorrectMappings()
    {
        // Arrange
        var request = new CreateBookProfileRequest
        {
            Title = "Advanced Programming Techniques",
            Author = "John Doe",
            ISBN = "978-0123456789",
            Category = BookCategory.Technical,
            Price = 49.99m,
            PublishedDate = DateTime.UtcNow.AddYears(-2),
            CoverImageUrl = "https://example.com/cover.jpg",
            StockQuantity = 15
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        
        // Get the created result
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<BookProfileDto>>(result);
        var bookDto = createdResult.Value;

        Assert.NotNull(bookDto);
        
        // Check CategoryDisplayName
        Assert.Equal("Technical & Professional", bookDto.CategoryDisplayName);

        // Check AuthorInitials for two-name author (John Doe -> JD)
        Assert.Equal("JD", bookDto.AuthorInitials);

        // Check PublishedAge calculation (2 years old)
        Assert.Contains("2 years old", bookDto.PublishedAge);

        // Check FormattedPrice starts with currency symbol
        Assert.StartsWith("$", bookDto.FormattedPrice);
        Assert.Contains("49.99", bookDto.FormattedPrice);

        // Check AvailabilityStatus based on stock (>5 = "In Stock")
        Assert.Equal("In Stock", bookDto.AvailabilityStatus);

        // Verify BookCreationStarted log called once
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == LogEvents.BookCreationStarted),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateISBN_ReturnsConflictWithLogging()
    {
        // Arrange - Create existing book in database with specific ISBN
        var existingBook = new Book
        {
            Id = Guid.NewGuid(),
            Title = "Existing Book",
            Author = "Jane Smith",
            ISBN = "978-1234567890",
            Category = BookCategory.Fiction,
            Price = 29.99m,
            PublishedDate = DateTime.UtcNow.AddYears(-1),
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.Books.Add(existingBook);
        await _context.SaveChangesAsync();

        // Arrange - Create request with same ISBN
        var request = new CreateBookProfileRequest
        {
            Title = "New Book",
            Author = "John Doe",
            ISBN = "978-1234567890", // Same ISBN
            Category = BookCategory.Technical,
            Price = 49.99m,
            PublishedDate = DateTime.UtcNow,
            StockQuantity = 5
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert - Check ValidationProblem result (validator catches duplicate ISBN)
        var validationResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(result);
        Assert.NotNull(validationResult.ProblemDetails);
        Assert.Equal(400, validationResult.ProblemDetails.Status);

        // Verify validation logging was performed
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                LogEvents.BookValidationFailed,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ChildrensBookRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange - Create valid Children's book request
        var request = new CreateBookProfileRequest
        {
            Title = "Fun Stories for Kids",
            Author = "Mary Johnson",
            ISBN = "978-9876543210",
            Category = BookCategory.Children,
            Price = 20.00m, // Original price
            PublishedDate = DateTime.UtcNow.AddMonths(-6),
            CoverImageUrl = "https://example.com/kids-cover.png",
            StockQuantity = 3
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<BookProfileDto>>(result);
        var bookDto = createdResult.Value;

        Assert.NotNull(bookDto);

        // Check CategoryDisplayName
        Assert.Equal("Children's Books", bookDto.CategoryDisplayName);

        // Check Price has 10% discount applied (20.00 * 0.9 = 18.00)
        Assert.Equal(18.00m, bookDto.Price);
        Assert.Contains("18.00", bookDto.FormattedPrice);

        // Check CoverImageUrl is null (content filtering for children's books)
        Assert.Null(bookDto.CoverImageUrl);

        // Verify book creation was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == LogEvents.BookCreationStarted),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _cache?.Dispose();
    }
}

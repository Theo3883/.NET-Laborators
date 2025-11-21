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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lab3.Tests.Integration;

/// <summary>
/// Integration tests for CreateOrderHandler with comprehensive validation,
/// AutoMapper profiles, caching, and logging verification
/// </summary>
public class CreateOrderHandlerIntegrationTests : IDisposable
{
    private readonly BookContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateOrderHandler>> _loggerMock;
    private readonly Mock<ILogger<OrderCacheService>> _cacheLoggerMock;
    private readonly CreateOrderHandler _handler;
    private readonly IValidator<CreateOrderProfileRequest> _validator;
    private readonly IOrderCacheService _cacheService;

    public CreateOrderHandlerIntegrationTests()
    {
        // Set up in-memory database with unique name
        var options = new DbContextOptionsBuilder<BookContext>()
            .UseInMemoryDatabase(databaseName: $"OrderTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new BookContext(options);

        // Configure AutoMapper with both order profiles (AdvancedOrderMappingProfile)
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedOrderMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Set up memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        // Mock ILogger for cache service
        _cacheLoggerMock = new Mock<ILogger<OrderCacheService>>();
        _cacheService = new OrderCacheService(_cache, _cacheLoggerMock.Object);
        
        // Mock IOrderMetricsService
        var metricsServiceMock = new Mock<IOrderMetricsService>();
        metricsServiceMock.Setup(m => m.RecordValidationTime(It.IsAny<double>()));
        metricsServiceMock.Setup(m => m.RecordDatabaseOperationTime(It.IsAny<double>()));

        // Mock ILogger<CreateOrderHandler>
        _loggerMock = new Mock<ILogger<CreateOrderHandler>>();

        // Create validator (for this test we'll use a mock to control validation)
        var validatorMock = new Mock<IValidator<CreateOrderProfileRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateOrderProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _validator = validatorMock.Object;

        // Create handler instance with all dependencies
        _handler = new CreateOrderHandler(
            _context,
            _validator,
            _mapper,
            _loggerMock.Object,
            _cacheService,
            metricsServiceMock.Object
        );
    }

    /// <summary>
    /// Test 1: Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMappings
    /// Verifies that a valid Technical order is created with proper AutoMapper transformations
    /// </summary>
    [Fact]
    public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMappings()
    {
        // Arrange: Create valid Technical order request with all properties
        var request = new CreateOrderProfileRequest(
            Title: "Advanced Software Architecture Patterns",
            Author: "Robert Martin",
            ISBN: "978-1-234-56789-0",
            Category: "Technical",
            Price: 59.99m,
            PublishedDate: new DateTime(2023, 1, 15),
            StockQuantity: 50,
            CoverImageUrl: "https://example.com/software.jpg"
        );

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-001");

        // Act: Call handler
        var result = await _handler.Handle(request, httpContext.Object);

        // Assert: Verify Created result type
        Assert.NotNull(result);
        
        // The handler returns Results<Created<OrderProfileDto>, ValidationProblem, Conflict<object>>
        // We need to access the Result property and cast appropriately
        var resultObject = result.Result;
        var createdResult = Assert.IsType<Created<OrderProfileDto>>(resultObject);
        var order = createdResult.Value;

        Assert.NotNull(order);

        // Assert: Check CategoryDisplayName = "Technical & Professional"
        Assert.Equal("Technical & Professional", order.CategoryDisplayName);

        // Assert: Check AuthorInitials for two-name author
        Assert.Equal("RM", order.AuthorInitials);

        // Assert: Check PublishedAge calculation
        Assert.NotNull(order.PublishedAge);
        Assert.Contains("year", order.PublishedAge.ToLower());

        // Assert: Check FormattedPrice starts with currency symbol
        Assert.NotNull(order.FormattedPrice);
        Assert.StartsWith("$", order.FormattedPrice);
        Assert.Contains("59.99", order.FormattedPrice);

        // Assert: Check AvailabilityStatus based on stock
        Assert.Equal("In Stock", order.AvailabilityStatus);
        Assert.True(order.IsAvailable);

        // Assert: Verify OrderCreationStarted log called once
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                LogEvents.OrderCreationStarted,
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "OrderCreationStarted should be logged once");
    }

    /// <summary>
    /// Test 2: Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging
    /// Verifies that duplicate ISBN validation is enforced with proper exception and logging
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
    {
        // Arrange: Create existing order in database with specific ISBN
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(),
            Title = "Existing Book",
            Author = "Existing Author",
            ISBN = "978-1-111-11111-1",
            Category = OrderCategory.Fiction,
            Price = 29.99m,
            PublishedDate = new DateTime(2022, 1, 1),
            StockQuantity = 20,
            CoverImageUrl = "https://example.com/existing.jpg",
            CreatedAt = DateTime.UtcNow
        };
        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();

        // Arrange: Create request with same ISBN
        var request = new CreateOrderProfileRequest(
            Title: "New Book",
            Author: "New Author",
            ISBN: "978-1-111-11111-1", // Duplicate ISBN
            Category: "Fiction",
            Price: 39.99m,
            PublishedDate: new DateTime(2023, 1, 1),
            StockQuantity: 30,
            CoverImageUrl: "https://example.com/new.jpg"
        );

        // Create a real validator for this test
        var validatorLogger = new Mock<ILogger<Validators.CreateOrderProfileValidator>>();
        var realValidator = new Validators.CreateOrderProfileValidator(_context, validatorLogger.Object);
        
        // Mock metrics service
        var metricsServiceMock = new Mock<IOrderMetricsService>();
        metricsServiceMock.Setup(m => m.RecordValidationTime(It.IsAny<double>()));
        metricsServiceMock.Setup(m => m.RecordDatabaseOperationTime(It.IsAny<double>()));

        var handlerWithRealValidator = new CreateOrderHandler(
            _context,
            realValidator,
            _mapper,
            _loggerMock.Object,
            _cacheService,
            metricsServiceMock.Object
        );

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-002");

        // Act: Call handler - should return ValidationProblem, not throw exception
        var result = await handlerWithRealValidator.Handle(request, httpContext.Object);

        // Assert: Verify ValidationProblem result type
        Assert.NotNull(result);
        var resultObject = result.Result;
        var validationProblem = Assert.IsType<ValidationProblem>(resultObject);

        // Assert: Check validation errors contain "already exists"
        Assert.NotNull(validationProblem.ProblemDetails);
        var problemDetails = validationProblem.ProblemDetails;
        var errorsJson = System.Text.Json.JsonSerializer.Serialize(problemDetails.Errors);
        Assert.Contains("already exists", errorsJson.ToLower());

        // Assert: Verify OrderValidationFailed log called once
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                LogEvents.OrderValidationFailed,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "OrderValidationFailed should be logged once");
    }

    /// <summary>
    /// Test 3: Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping
    /// Verifies that Children's orders get 10% discount and content filtering applied
    /// </summary>
    [Fact]
    public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange: Create valid Children's order request
        var request = new CreateOrderProfileRequest(
            Title: "Fun Stories for Kids",
            Author: "Mary Johnson",
            ISBN: "978-2-222-22222-2",
            Category: "Children",
            Price: 30.00m,
            PublishedDate: new DateTime(2023, 6, 1),
            StockQuantity: 100,
            CoverImageUrl: "https://example.com/kids.jpg"
        );

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-003");

        // Act: Call handler
        var result = await _handler.Handle(request, httpContext.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var createdResult = Assert.IsType<Created<OrderProfileDto>>(resultObject);
        var order = createdResult.Value;

        Assert.NotNull(order);

        // Assert: Check CategoryDisplayName = "Children's Orders"
        Assert.Equal("Children's Orders", order.CategoryDisplayName);

        // Assert: Check Price has 10% discount applied
        // Original: $30.00, Expected after 10% discount: $27.00
        Assert.Equal(27.00m, order.Price);

        // Assert: Check CoverImageUrl is null (content filtering)
        Assert.Null(order.CoverImageUrl);
    }

    /// <summary>
    /// Proper disposal of context and cache
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
        _cache?.Dispose();
        GC.SuppressFinalize(this);
    }
}

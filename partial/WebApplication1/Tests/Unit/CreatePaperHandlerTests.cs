using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.DTO.Request;
using WebApplication1.Handlers;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Xunit;

namespace WebApplication1.Tests.Unit;

/// <summary>
/// Unit tests for CreatePaperHandler with mocked dependencies
/// Tests handler behavior, database interactions, and validation integration
/// </summary>
public class CreatePaperHandlerTests : IDisposable
{
    private readonly PaperContext _context;
    private readonly Mock<IValidator<CreatePaperRequest>> _validatorMock;
    private readonly Mock<ILogger<CreatePaperHandler>> _loggerMock;
    private readonly CreatePaperHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;

    public CreatePaperHandlerTests()
    {
        // Setup in-memory database with unique name for isolation
        var options = new DbContextOptionsBuilder<PaperContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new PaperContext(options);

        // Setup mocks
        _validatorMock = new Mock<IValidator<CreatePaperRequest>>();
        _loggerMock = new Mock<ILogger<CreatePaperHandler>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextMock.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-001");

        // Create handler with mocked dependencies
        _handler = new CreatePaperHandler(_context, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPaperSuccessfully()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Test Paper",
            Author: "Test Author",
            PublishedOn: new DateTime(2024, 1, 15)
        );

        // Mock validator to return valid result
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var createdResult = Assert.IsType<Created<Paper>>(resultObject);
        var paper = createdResult.Value;

        Assert.NotNull(paper);
        Assert.Equal("Test Paper", paper.Title);
        Assert.Equal("Test Author", paper.Author);
        Assert.Equal(new DateTime(2024, 1, 15), paper.PublishedOn);
        Assert.True(paper.Id > 0); // Verify ID was assigned

        // Verify paper was saved to database
        var savedPaper = await _context.Papers.FindAsync(paper.Id);
        Assert.NotNull(savedPaper);
        Assert.Equal("Test Paper", savedPaper.Title);

        // Verify validator was called
        _validatorMock.Verify(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Creating paper")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "", // Invalid - empty title
            Author: "Test Author",
            PublishedOn: new DateTime(2024, 1, 15)
        );

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Title", "Title is required and cannot be empty")
        };
        var validationResult = new ValidationResult(validationFailures);

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var validationProblem = Assert.IsType<ValidationProblem>(resultObject);

        Assert.NotNull(validationProblem.ProblemDetails);
        Assert.Contains("Title", validationProblem.ProblemDetails.Errors.Keys);

        // Verify no paper was saved to database
        var paperCount = await _context.Papers.CountAsync();
        Assert.Equal(0, paperCount);

        // Verify validator was called
        _validatorMock.Verify(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "", // Invalid
            Author: "", // Invalid
            PublishedOn: DateTime.Today.AddDays(1) // Invalid - future date
        );

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Title", "Title is required and cannot be empty"),
            new ValidationFailure("Author", "Author is required and cannot be empty"),
            new ValidationFailure("PublishedOn", "Published date cannot be in the future")
        };
        var validationResult = new ValidationResult(validationFailures);

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var validationProblem = Assert.IsType<ValidationProblem>(resultObject);

        Assert.NotNull(validationProblem.ProblemDetails);
        Assert.Equal(3, validationProblem.ProblemDetails.Errors.Count);
        Assert.Contains("Title", validationProblem.ProblemDetails.Errors.Keys);
        Assert.Contains("Author", validationProblem.ProblemDetails.Errors.Keys);
        Assert.Contains("PublishedOn", validationProblem.ProblemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsCorrectLocationHeader()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Location Test Paper",
            Author: "Location Test Author",
            PublishedOn: new DateTime(2024, 5, 20)
        );

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var createdResult = Assert.IsType<Created<Paper>>(resultObject);

        Assert.NotNull(createdResult.Location);
        Assert.StartsWith("/papers/", createdResult.Location);
        Assert.Contains(createdResult.Value!.Id.ToString(), createdResult.Location);
    }

    [Fact]
    public async Task Handle_PaperWithMaxLengthFields_SavesSuccessfully()
    {
        // Arrange
        var longTitle = new string('A', 200); // Max length
        var longAuthor = new string('B', 100); // Max length

        var request = new CreatePaperRequest(
            Title: longTitle,
            Author: longAuthor,
            PublishedOn: DateTime.Today
        );

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var createdResult = Assert.IsType<Created<Paper>>(resultObject);
        var paper = createdResult.Value;

        Assert.NotNull(paper);
        Assert.Equal(200, paper.Title.Length);
        Assert.Equal(100, paper.Author.Length);

        // Verify saved to database
        var savedPaper = await _context.Papers.FindAsync(paper.Id);
        Assert.NotNull(savedPaper);
        Assert.Equal(longTitle, savedPaper.Title);
        Assert.Equal(longAuthor, savedPaper.Author);
    }

    [Fact]
    public async Task Handle_TraceIdIncluded_InValidationProblem()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "",
            Author: "Test Author",
            PublishedOn: DateTime.Today
        );

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Title", "Title is required and cannot be empty")
        };
        var validationResult = new ValidationResult(validationFailures);

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var validationProblem = Assert.IsType<ValidationProblem>(resultObject);

        Assert.NotNull(validationProblem.ProblemDetails);
        Assert.True(validationProblem.ProblemDetails.Extensions.ContainsKey("traceId"));
        Assert.Equal("TEST-TRACE-001", validationProblem.ProblemDetails.Extensions["traceId"]);
    }

    [Fact]
    public async Task Handle_LogsCreationWithCorrectParameters()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Logging Test Paper",
            Author: "Logging Test Author",
            PublishedOn: new DateTime(2024, 3, 10)
        );

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        await _handler.Handle(request, _httpContextMock.Object);

        // Assert - Verify logging was called with correct parameters
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Logging Test Paper")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Logging Test Author")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}


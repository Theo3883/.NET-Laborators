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
/// Unit tests for GetPaperByIdHandler with mocked dependencies
/// Tests retrieval scenarios including success and 404 not found cases
/// </summary>
public class GetPaperByIdHandlerTests : IDisposable
{
    private readonly PaperContext _context;
    private readonly Mock<ILogger<GetPaperByIdHandler>> _loggerMock;
    private readonly GetPaperByIdHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;

    public GetPaperByIdHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PaperContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new PaperContext(options);

        // Setup mocks
        _loggerMock = new Mock<ILogger<GetPaperByIdHandler>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextMock.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-001");

        // Create handler
        _handler = new GetPaperByIdHandler(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPaperId_ReturnsPaper()
    {
        // Arrange
        var paper = new Paper
        {
            Title = "Existing Paper",
            Author = "Existing Author",
            PublishedOn = new DateTime(2024, 1, 15)
        };
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();

        var request = new GetPaperByIdRequest(paper.Id);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var okResult = Assert.IsType<Ok<Paper>>(resultObject);
        var returnedPaper = okResult.Value;

        Assert.NotNull(returnedPaper);
        Assert.Equal(paper.Id, returnedPaper.Id);
        Assert.Equal("Existing Paper", returnedPaper.Title);
        Assert.Equal("Existing Author", returnedPaper.Author);
        Assert.Equal(new DateTime(2024, 1, 15), returnedPaper.PublishedOn);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("retrieved successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentPaperId_ReturnsNotFound()
    {
        // Arrange
        var request = new GetPaperByIdRequest(999); // Non-existent ID

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        Assert.IsType<NotFound>(resultObject);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroId_ReturnsNotFound()
    {
        // Arrange
        var request = new GetPaperByIdRequest(0);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        Assert.IsType<NotFound>(resultObject);
    }

    [Fact]
    public async Task Handle_NegativeId_ReturnsNotFound()
    {
        // Arrange
        var request = new GetPaperByIdRequest(-1);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        Assert.IsType<NotFound>(resultObject);
    }

    [Fact]
    public async Task Handle_MultiplePapersInDb_ReturnsCorrectOne()
    {
        // Arrange - Add multiple papers
        var paper1 = new Paper { Title = "Paper 1", Author = "Author 1", PublishedOn = DateTime.Today };
        var paper2 = new Paper { Title = "Paper 2", Author = "Author 2", PublishedOn = DateTime.Today };
        var paper3 = new Paper { Title = "Paper 3", Author = "Author 3", PublishedOn = DateTime.Today };

        _context.Papers.AddRange(paper1, paper2, paper3);
        await _context.SaveChangesAsync();

        var request = new GetPaperByIdRequest(paper2.Id);

        // Act
        var result = await _handler.Handle(request, _httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var resultObject = result.Result;
        var okResult = Assert.IsType<Ok<Paper>>(resultObject);
        var returnedPaper = okResult.Value;

        Assert.NotNull(returnedPaper);
        Assert.Equal(paper2.Id, returnedPaper.Id);
        Assert.Equal("Paper 2", returnedPaper.Title);
        Assert.Equal("Author 2", returnedPaper.Author);
    }

    [Fact]
    public async Task Handle_LogsWithTraceId()
    {
        // Arrange
        var paper = new Paper
        {
            Title = "Trace Test Paper",
            Author = "Trace Test Author",
            PublishedOn = DateTime.Today
        };
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();

        var request = new GetPaperByIdRequest(paper.Id);

        // Act
        await _handler.Handle(request, _httpContextMock.Object);

        // Assert - Verify trace ID is logged
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TEST-TRACE-001")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_DoesNotTrackEntity()
    {
        // Arrange
        var paper = new Paper
        {
            Title = "No Tracking Paper",
            Author = "No Tracking Author",
            PublishedOn = DateTime.Today
        };
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var request = new GetPaperByIdRequest(paper.Id);

        // Act
        await _handler.Handle(request, _httpContextMock.Object);

        // Assert - Verify entity is not tracked
        var trackedEntities = _context.ChangeTracker.Entries<Paper>().ToList();
        Assert.Empty(trackedEntities);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}


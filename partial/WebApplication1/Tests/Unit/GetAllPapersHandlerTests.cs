using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Handlers;
using WebApplication1.Model;
using WebApplication1.Persistence;
using Xunit;

namespace WebApplication1.Tests.Unit;

/// <summary>
/// Unit tests for GetAllPapersHandler with mocked dependencies
/// Tests retrieval of all papers with proper ordering
/// </summary>
public class GetAllPapersHandlerTests : IDisposable
{
    private readonly PaperContext _context;
    private readonly Mock<ILogger<GetAllPapersHandler>> _loggerMock;
    private readonly GetAllPapersHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;

    public GetAllPapersHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PaperContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new PaperContext(options);

        // Setup mocks
        _loggerMock = new Mock<ILogger<GetAllPapersHandler>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextMock.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-001");

        // Create handler
        _handler = new GetAllPapersHandler(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Empty(papers);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieved 0 papers")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultiplePapers_ReturnsAllPapers()
    {
        // Arrange - Add papers
        var paper1 = new Paper { Title = "Paper 1", Author = "Author 1", PublishedOn = new DateTime(2023, 1, 1) };
        var paper2 = new Paper { Title = "Paper 2", Author = "Author 2", PublishedOn = new DateTime(2023, 6, 1) };
        var paper3 = new Paper { Title = "Paper 3", Author = "Author 3", PublishedOn = new DateTime(2024, 1, 1) };

        _context.Papers.AddRange(paper1, paper2, paper3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Equal(3, papers.Count);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieved 3 papers")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultiplePapers_OrderedByPublishedOnDescending()
    {
        // Arrange - Add papers in random order
        var paper1 = new Paper { Title = "Oldest", Author = "Author 1", PublishedOn = new DateTime(2022, 1, 1) };
        var paper2 = new Paper { Title = "Newest", Author = "Author 2", PublishedOn = new DateTime(2024, 12, 1) };
        var paper3 = new Paper { Title = "Middle", Author = "Author 3", PublishedOn = new DateTime(2023, 6, 15) };

        _context.Papers.AddRange(paper1, paper2, paper3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Equal(3, papers.Count);

        // Verify ordering - newest first
        Assert.Equal("Newest", papers[0].Title);
        Assert.Equal(new DateTime(2024, 12, 1), papers[0].PublishedOn);

        Assert.Equal("Middle", papers[1].Title);
        Assert.Equal(new DateTime(2023, 6, 15), papers[1].PublishedOn);

        Assert.Equal("Oldest", papers[2].Title);
        Assert.Equal(new DateTime(2022, 1, 1), papers[2].PublishedOn);
    }

    [Fact]
    public async Task Handle_PapersWithSamePublishedDate_ReturnsAllOfThem()
    {
        // Arrange - Add papers with same date
        var sameDate = new DateTime(2024, 5, 15);
        var paper1 = new Paper { Title = "Paper A", Author = "Author 1", PublishedOn = sameDate };
        var paper2 = new Paper { Title = "Paper B", Author = "Author 2", PublishedOn = sameDate };
        var paper3 = new Paper { Title = "Paper C", Author = "Author 3", PublishedOn = sameDate };

        _context.Papers.AddRange(paper1, paper2, paper3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Equal(3, papers.Count);
        Assert.All(papers, p => Assert.Equal(sameDate, p.PublishedOn));
    }

    [Fact]
    public async Task Handle_SinglePaper_ReturnsList()
    {
        // Arrange
        var paper = new Paper
        {
            Title = "Only Paper",
            Author = "Only Author",
            PublishedOn = new DateTime(2024, 3, 20)
        };
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Single(papers);
        Assert.Equal("Only Paper", papers[0].Title);
    }

    [Fact]
    public async Task Handle_LogsWithTraceId()
    {
        // Act
        await _handler.Handle(_httpContextMock.Object);

        // Assert - Verify trace ID is logged
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TEST-TRACE-001")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotTrackEntities()
    {
        // Arrange
        var paper = new Paper { Title = "No Tracking", Author = "Author", PublishedOn = DateTime.Today };
        _context.Papers.Add(paper);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        await _handler.Handle(_httpContextMock.Object);

        // Assert - Verify entities are not tracked (AsNoTracking was used)
        var trackedEntities = _context.ChangeTracker.Entries<Paper>().ToList();
        Assert.Empty(trackedEntities);
    }

    [Fact]
    public async Task Handle_LargeNumberOfPapers_ReturnsAll()
    {
        // Arrange - Add 100 papers
        var papers = Enumerable.Range(1, 100).Select(i => new Paper
        {
            Title = $"Paper {i}",
            Author = $"Author {i}",
            PublishedOn = DateTime.Today.AddDays(-i)
        }).ToList();

        _context.Papers.AddRange(papers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var returnedPapers = okResult.Value;

        Assert.NotNull(returnedPapers);
        Assert.Equal(100, returnedPapers.Count);

        // Verify first is most recent (published today minus 1 day)
        Assert.Equal("Paper 1", returnedPapers[0].Title);
        
        // Verify last is oldest (published today minus 100 days)
        Assert.Equal("Paper 100", returnedPapers[99].Title);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}


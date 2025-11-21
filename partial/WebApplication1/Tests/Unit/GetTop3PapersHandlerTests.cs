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
/// Unit tests for GetTop3PapersHandler with mocked dependencies
/// Tests retrieval of top 3 most recent papers with proper ordering and edge cases
/// </summary>
public class GetTop3PapersHandlerTests : IDisposable
{
    private readonly PaperContext _context;
    private readonly Mock<ILogger<GetTop3PapersHandler>> _loggerMock;
    private readonly GetTop3PapersHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;

    public GetTop3PapersHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PaperContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new PaperContext(options);

        // Setup mocks
        _loggerMock = new Mock<ILogger<GetTop3PapersHandler>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextMock.Setup(x => x.TraceIdentifier).Returns("TEST-TRACE-001");

        // Create handler
        _handler = new GetTop3PapersHandler(_context, _loggerMock.Object);
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
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieved top 0 papers")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LessThan3Papers_ReturnsAllAvailable()
    {
        // Arrange - Add only 2 papers
        var paper1 = new Paper { Title = "Paper 1", Author = "Author 1", PublishedOn = new DateTime(2024, 1, 1) };
        var paper2 = new Paper { Title = "Paper 2", Author = "Author 2", PublishedOn = new DateTime(2024, 6, 1) };

        _context.Papers.AddRange(paper1, paper2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Equal(2, papers.Count);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieved top 2 papers")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Exactly3Papers_ReturnsAll3()
    {
        // Arrange - Add exactly 3 papers
        var paper1 = new Paper { Title = "Paper 1", Author = "Author 1", PublishedOn = new DateTime(2024, 1, 1) };
        var paper2 = new Paper { Title = "Paper 2", Author = "Author 2", PublishedOn = new DateTime(2024, 6, 1) };
        var paper3 = new Paper { Title = "Paper 3", Author = "Author 3", PublishedOn = new DateTime(2024, 12, 1) };

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
    }

    [Fact]
    public async Task Handle_MoreThan3Papers_ReturnsOnly3MostRecent()
    {
        // Arrange - Add 10 papers
        var papers = new List<Paper>
        {
            new Paper { Title = "Oldest", Author = "Author", PublishedOn = new DateTime(2020, 1, 1) },
            new Paper { Title = "Old 2", Author = "Author", PublishedOn = new DateTime(2021, 1, 1) },
            new Paper { Title = "Old 3", Author = "Author", PublishedOn = new DateTime(2022, 1, 1) },
            new Paper { Title = "Old 4", Author = "Author", PublishedOn = new DateTime(2023, 1, 1) },
            new Paper { Title = "Recent 3", Author = "Author", PublishedOn = new DateTime(2024, 1, 1) },
            new Paper { Title = "Recent 2", Author = "Author", PublishedOn = new DateTime(2024, 6, 1) },
            new Paper { Title = "Most Recent", Author = "Author", PublishedOn = new DateTime(2024, 11, 20) }
        };

        _context.Papers.AddRange(papers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var returnedPapers = okResult.Value;

        Assert.NotNull(returnedPapers);
        Assert.Equal(3, returnedPapers.Count);

        // Verify it's the 3 most recent papers in descending order
        Assert.Equal("Most Recent", returnedPapers[0].Title);
        Assert.Equal(new DateTime(2024, 11, 20), returnedPapers[0].PublishedOn);

        Assert.Equal("Recent 2", returnedPapers[1].Title);
        Assert.Equal(new DateTime(2024, 6, 1), returnedPapers[1].PublishedOn);

        Assert.Equal("Recent 3", returnedPapers[2].Title);
        Assert.Equal(new DateTime(2024, 1, 1), returnedPapers[2].PublishedOn);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieved top 3 papers")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Papers_OrderedByPublishedOnDescending()
    {
        // Arrange - Add papers in random order
        var paper1 = new Paper { Title = "Third", Author = "Author", PublishedOn = new DateTime(2024, 3, 1) };
        var paper2 = new Paper { Title = "First", Author = "Author", PublishedOn = new DateTime(2024, 9, 1) };
        var paper3 = new Paper { Title = "Second", Author = "Author", PublishedOn = new DateTime(2024, 6, 1) };
        var paper4 = new Paper { Title = "Fourth (not returned)", Author = "Author", PublishedOn = new DateTime(2023, 1, 1) };

        _context.Papers.AddRange(paper1, paper2, paper3, paper4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<Ok<List<Paper>>>(result);
        var papers = okResult.Value;

        Assert.NotNull(papers);
        Assert.Equal(3, papers.Count);

        // Verify correct ordering
        Assert.Equal("First", papers[0].Title);
        Assert.Equal("Second", papers[1].Title);
        Assert.Equal("Third", papers[2].Title);

        // Verify oldest paper is not included
        Assert.DoesNotContain(papers, p => p.Title == "Fourth (not returned)");
    }

    [Fact]
    public async Task Handle_SinglePaper_ReturnsSingleItem()
    {
        // Arrange
        var paper = new Paper
        {
            Title = "Only Paper",
            Author = "Only Author",
            PublishedOn = new DateTime(2024, 5, 15)
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
    public async Task Handle_PapersWithSameDate_AllIncludedIfTop3()
    {
        // Arrange - 3 papers with same date, 1 older paper
        var sameDate = new DateTime(2024, 10, 1);
        var paper1 = new Paper { Title = "Same Date A", Author = "Author", PublishedOn = sameDate };
        var paper2 = new Paper { Title = "Same Date B", Author = "Author", PublishedOn = sameDate };
        var paper3 = new Paper { Title = "Same Date C", Author = "Author", PublishedOn = sameDate };
        var paper4 = new Paper { Title = "Older", Author = "Author", PublishedOn = new DateTime(2023, 1, 1) };

        _context.Papers.AddRange(paper1, paper2, paper3, paper4);
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
        Assert.DoesNotContain(papers, p => p.Title == "Older");
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

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}


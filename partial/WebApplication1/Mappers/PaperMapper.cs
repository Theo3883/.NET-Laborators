using WebApplication1.DTO.Request;
using WebApplication1.Model;

namespace WebApplication1.Mappers;

/// <summary>
/// Concrete implementation of IPaperMapper
/// </summary>
public class PaperMapper : IPaperMapper
{
    public Paper MapToEntity(CreatePaperRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Paper
        {
            Title = request.Title,
            Author = request.Author,
            PublishedOn = request.PublishedOn
        };
    }

    public Paper MapToResponse(Paper paper)
    {
        // Currently returning entity as-is
        return paper ?? throw new ArgumentNullException(nameof(paper));
    }
}


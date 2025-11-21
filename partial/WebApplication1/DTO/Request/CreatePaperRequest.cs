namespace WebApplication1.DTO.Request;

public record CreatePaperRequest(
    string Title,
    string Author,
    DateTime PublishedOn
);


namespace Lab3.DTO.Request;

public record CreateOrderProfileRequest(
    string Title,
    string Author,
    string ISBN,
    string Category,
    decimal Price,
    DateTime PublishedDate,
    int StockQuantity,
    string? CoverImageUrl
);

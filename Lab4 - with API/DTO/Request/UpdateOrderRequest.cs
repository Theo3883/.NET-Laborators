namespace Lab3.DTO.Request;

public record UpdateOrderRequest
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string ISBN { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime PublishedDate { get; init; }
    public int StockQuantity { get; init; }
    public string? CoverImageUrl { get; init; }
}

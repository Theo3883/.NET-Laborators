using Lab3.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Lab3.DTO.Request;

public record UpdateOrderRequest
{
    public string Id { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Author is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Author must be between 1 and 100 characters")]
    public string Author { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "ISBN is required")]
    [ValidISBN] // Custom attribute: Validates 10 or 13 digits format
    public string ISBN { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Category is required")]
    [OrderCategory("Fiction", "NonFiction", "Technical", "Children")] // Custom attribute: Validates allowed categories
    public string Category { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Price is required")]
    [PriceRange(0.01, 10000.00)] // Custom attribute: Validates price range with currency formatting
    public decimal Price { get; init; }
    
    public DateTime PublishedDate { get; init; }
    
    [Range(0, 100000, ErrorMessage = "Stock quantity must be between 0 and 100,000")]
    public int StockQuantity { get; init; }
    
    public string? CoverImageUrl { get; init; }
}

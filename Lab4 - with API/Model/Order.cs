using System.ComponentModel.DataAnnotations;

namespace Lab3.Model;

public class Order
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    public OrderCategory Category { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public DateTime PublishedDate { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public int StockQuantity { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; }

    // Computed property based on stock quantity
    public bool IsAvailable => StockQuantity > 0;
}

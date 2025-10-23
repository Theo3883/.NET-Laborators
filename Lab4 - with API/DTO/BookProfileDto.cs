using Lab3.Model;

namespace Lab3.DTO;

public class BookProfileDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string PublishedAge { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = string.Empty;

    public static BookProfileDto FromBook(Book book)
    {
        var age = DateTime.UtcNow.Year - book.PublishedDate.Year;
        var ageText = age switch
        {
            0 => "Published this year",
            1 => "1 year old",
            _ => $"{age} years old"
        };

        var initials = string.Join("", 
            book.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word[0].ToString().ToUpper())
        );

        var status = book.IsAvailable 
            ? $"In Stock ({book.StockQuantity} available)" 
            : "Out of Stock";

        return new BookProfileDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            CategoryDisplayName = book.Category.ToString(),
            Price = book.Price,
            FormattedPrice = $"${book.Price:F2}",
            PublishedDate = book.PublishedDate,
            CreatedAt = book.CreatedAt,
            CoverImageUrl = book.CoverImageUrl,
            IsAvailable = book.IsAvailable,
            StockQuantity = book.StockQuantity,
            PublishedAge = ageText,
            AuthorInitials = initials,
            AvailabilityStatus = status
        };
    }
}

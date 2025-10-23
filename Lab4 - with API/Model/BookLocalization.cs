namespace Lab3.Model;


/// Localized metadata for books
public class BookLocalization
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string CultureCode { get; set; } = "en-US";
    public string LocalizedTitle { get; set; } = string.Empty;
    public string? LocalizedDescription { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Book Book { get; set; } = null!;
}

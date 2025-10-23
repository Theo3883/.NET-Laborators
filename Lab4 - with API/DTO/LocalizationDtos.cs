namespace Lab3.DTO;


/// DTO for book localization information
public record BookLocalizationDto(
    string CultureCode,
    string LocalizedTitle,
    string? LocalizedDescription
);

/// Request to create or update a book localization
public record CreateBookLocalizationRequest(
    string CultureCode,
    string LocalizedTitle,
    string? LocalizedDescription = null
);

/// Enhanced book DTO with localization support
public record LocalizedBookDto(
    Guid Id,
    string Title,
    string Author,
    string ISBN,
    string Category,
    string CategoryDescription,
    decimal Price,
    string FormattedPrice,
    DateTime PublishedDate,
    DateTime CreatedAt,
    string? CoverImageUrl,
    bool IsAvailable,
    int StockQuantity,
    string AvailabilityStatus,
    string PublishedAge,
    string AuthorInitials,
    string Culture,
    List<string> AvailableCultures
);

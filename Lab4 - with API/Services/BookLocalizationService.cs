using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Services;


/// Service for managing book-specific localizations (titles and descriptions)
public interface IBookLocalizationService
{
    Task<string> GetLocalizedTitleAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default);
    Task<string?> GetLocalizedDescriptionAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default);
    Task<BookLocalization?> GetLocalizationAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default);
    Task<List<BookLocalization>> GetAllLocalizationsAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<BookLocalization> CreateOrUpdateLocalizationAsync(Guid bookId, string cultureCode, string localizedTitle, string? localizedDescription = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteLocalizationAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default);
}

public class BookLocalizationService : IBookLocalizationService
{
    private readonly BookContext _context;
    private readonly ILogger<BookLocalizationService> _logger;
    private const string DefaultCulture = "en-US";

    public BookLocalizationService(BookContext context, ILogger<BookLocalizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GetLocalizedTitleAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default)
    {
        // Try to get localized title
        var localization = await GetLocalizationAsync(bookId, cultureCode, cancellationToken);
        if (localization != null)
        {
            _logger.LogDebug("Found localized title for book {BookId} in culture {Culture}", bookId, cultureCode);
            return localization.LocalizedTitle;
        }

        // Fallback to default language (book's original title)
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

        if (book == null)
        {
            _logger.LogWarning("Book {BookId} not found", bookId);
            return string.Empty;
        }

        _logger.LogDebug("Falling back to default title for book {BookId}", bookId);
        return book.Title;
    }

    public async Task<string?> GetLocalizedDescriptionAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default)
    {
        var localization = await GetLocalizationAsync(bookId, cultureCode, cancellationToken);
        return localization?.LocalizedDescription;
    }

    public async Task<BookLocalization?> GetLocalizationAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default)
    {
        var normalizedCulture = NormalizeCulture(cultureCode);
        
        // Try exact match
        var localization = await _context.BookLocalizations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.BookId == bookId && l.CultureCode == normalizedCulture,
                cancellationToken);

        if (localization != null)
            return localization;

        // Try language-only match (e.g., "es" for "es-MX")
        var languageCode = normalizedCulture.Split('-')[0];
        if (languageCode != normalizedCulture)
        {
            localization = await _context.BookLocalizations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.BookId == bookId && l.CultureCode.StartsWith(languageCode),
                    cancellationToken);
        }

        return localization;
    }

    public async Task<List<BookLocalization>> GetAllLocalizationsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookLocalizations
            .AsNoTracking()
            .Where(l => l.BookId == bookId)
            .OrderBy(l => l.CultureCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<BookLocalization> CreateOrUpdateLocalizationAsync(
        Guid bookId, 
        string cultureCode, 
        string localizedTitle, 
        string? localizedDescription = null, 
        CancellationToken cancellationToken = default)
    {
        // Validate book exists
        var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId, cancellationToken);
        if (!bookExists)
        {
            throw new InvalidOperationException($"Book with ID {bookId} does not exist");
        }

        var normalizedCulture = NormalizeCulture(cultureCode);

        // Check if localization already exists
        var existing = await _context.BookLocalizations
            .FirstOrDefaultAsync(
                l => l.BookId == bookId && l.CultureCode == normalizedCulture,
                cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.LocalizedTitle = localizedTitle;
            existing.LocalizedDescription = localizedDescription;
            existing.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Updated localization for book {BookId} in culture {Culture}", 
                bookId, normalizedCulture);
            
            return existing;
        }
        else
        {
            // Create new
            var localization = new BookLocalization
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                CultureCode = normalizedCulture,
                LocalizedTitle = localizedTitle,
                LocalizedDescription = localizedDescription,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BookLocalizations.Add(localization);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created localization for book {BookId} in culture {Culture}", 
                bookId, normalizedCulture);

            return localization;
        }
    }

    public async Task<bool> DeleteLocalizationAsync(Guid bookId, string cultureCode, CancellationToken cancellationToken = default)
    {
        var normalizedCulture = NormalizeCulture(cultureCode);
        
        var localization = await _context.BookLocalizations
            .FirstOrDefaultAsync(
                l => l.BookId == bookId && l.CultureCode == normalizedCulture,
                cancellationToken);

        if (localization == null)
            return false;

        _context.BookLocalizations.Remove(localization);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted localization for book {BookId} in culture {Culture}", 
            bookId, normalizedCulture);

        return true;
    }

    private static string NormalizeCulture(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return DefaultCulture;

        return cultureCode.ToLower() switch
        {
            "es-es" or "es-mx" => "es",
            "fr-fr" or "fr-ca" => "fr",
            "de-de" or "de-at" => "de",
            "ja-jp" => "ja",
            _ => cultureCode
        };
    }
}

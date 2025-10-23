using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Lab3.Validators;

public class CreateBookProfileValidator : AbstractValidator<CreateBookProfileRequest>
{
    private readonly BookContext _context;
    private readonly ILogger<CreateBookProfileValidator> _logger;

    private static readonly string[] InappropriateWords = { "damn", "hell", "crap", "offensive", "inappropriate" };
    private static readonly string[] ChildrenInappropriateWords = { "violence", "scary", "horror", "death", "blood" };
    private static readonly string[] TechnicalKeywords = { "programming", "algorithm", "software", "engineering", "technology", "computer", "data", "science", "technical", "guide" };

    public CreateBookProfileValidator(BookContext context, ILogger<CreateBookProfileValidator> logger)
    {
        this._context = context;
        this._logger = logger;

        // Title Validation
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(1).WithMessage("Title must be at least 1 character.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .Must(BeValidTitle).WithMessage("Title contains inappropriate content.")
            .MustAsync(BeUniqueTitle).WithMessage("A book with this title already exists for this author.");

        // Author Validation
        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MinimumLength(2).WithMessage("Author must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.")
            .Must(BeValidAuthorName).WithMessage("Author name contains invalid characters. Only letters, spaces, hyphens, apostrophes, and dots are allowed.");

        // ISBN Validation
        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .Must(BeValidISBN).WithMessage("ISBN must be a valid ISBN-10 or ISBN-13 format.")
            .MustAsync(BeUniqueISBN).WithMessage("A book with this ISBN already exists.");

        // Category Validation
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid BookCategory value.");

        // Price Validation
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThan(10000).WithMessage("Price must be less than $10,000.");

        // PublishedDate Validation
        RuleFor(x => x.PublishedDate)
            .NotEmpty().WithMessage("Published date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future.")
            .GreaterThanOrEqualTo(new DateTime(1400, 1, 1)).WithMessage("Published date cannot be before year 1400.");

        // StockQuantity Validation
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000.");

        // CoverImageUrl Validation
        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidImageUrl).When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("Cover image URL must be a valid HTTP/HTTPS image URL with extensions: .jpg, .jpeg, .png, .gif, .webp");

        // Business Rules Validation
        RuleFor(x => x)
            .MustAsync(PassBusinessRules).WithMessage("Book creation failed business rule validation.");

        // Conditional Validation - Technical Books
        When(x => x.Category == BookCategory.Technical, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(20).WithMessage("Technical books must have a minimum price of $20.00.");

            RuleFor(x => x.Title)
                .Must(ContainTechnicalKeywords).WithMessage("Technical book title must contain technical keywords.");

            RuleFor(x => x.PublishedDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-5))
                .WithMessage("Technical books must be published within the last 5 years.");
        });

        // Conditional Validation - Children's Books
        When(x => x.Category == BookCategory.Children, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(50).WithMessage("Children's books must have a maximum price of $50.00.");

            RuleFor(x => x.Title)
                .Must(BeAppropriateForChildren).WithMessage("Children's book title contains inappropriate content.");
        });

        // Conditional Validation - Fiction Books
        When(x => x.Category == BookCategory.Fiction, () =>
        {
            RuleFor(x => x.Author)
                .MinimumLength(5).WithMessage("Fiction books require full author name (minimum 5 characters).");
        });

        // Cross-Field Validation - Expensive Books Stock Limit
        RuleFor(x => x)
            .Must(x => x.Price <= 100 || x.StockQuantity <= 20)
            .WithMessage("Books priced over $100 must have limited stock (20 units or less).");

        // Cross-Field Validation - Technical Books Recent Publication
        RuleFor(x => x)
            .Must(x => x.Category != BookCategory.Technical || x.PublishedDate >= DateTime.UtcNow.AddYears(-5))
            .WithMessage("Technical books must be published within the last 5 years.");
    }

    // Validation Helper Methods

    private bool BeValidTitle(string title)
    {
        var titleLower = title.ToLower();
        return !InappropriateWords.Any(word => titleLower.Contains(word));
    }

    private async Task<bool> BeUniqueTitle(CreateBookProfileRequest request, string title, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking title uniqueness - Title: {Title}, Author: {Author}", title, request.Author);

        var exists = await _context.Books
            .AnyAsync(b => b.Title == title && b.Author == request.Author, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Duplicate title found - Title: {Title}, Author: {Author}", title, request.Author);
        }
        else
        {
            _logger.LogDebug("Title is unique - Title: {Title}, Author: {Author}", title, request.Author);
        }

        return !exists;
    }

    private bool BeValidAuthorName(string author)
    {
        // Allow letters, spaces, hyphens, apostrophes, dots
        var regex = new Regex(@"^[a-zA-Z\s\-'.]+$");
        return regex.IsMatch(author);
    }

    private bool BeValidISBN(string isbn)
    {
        // Remove hyphens and spaces
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Check for ISBN-10 (10 digits, last can be X)
        if (cleanIsbn.Length == 10)
        {
            return Regex.IsMatch(cleanIsbn, @"^\d{9}[\dX]$");
        }

        // Check for ISBN-13 (13 digits starting with 978 or 979)
        if (cleanIsbn.Length == 13)
        {
            return Regex.IsMatch(cleanIsbn, @"^(978|979)\d{10}$");
        }

        return false;
    }

    private async Task<bool> BeUniqueISBN(string isbn, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking ISBN uniqueness - ISBN: {ISBN}", isbn);

        var exists = await _context.Books.AnyAsync(b => b.ISBN == isbn, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Duplicate ISBN found - ISBN: {ISBN}", isbn);
        }
        else
        {
            _logger.LogDebug("ISBN is unique - ISBN: {ISBN}", isbn);
        }

        return !exists;
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        // Check if valid URL with HTTP/HTTPS protocol
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            return false;

        if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            return false;

        // Check for image extensions
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var path = uriResult.AbsolutePath.ToLower();
        return validExtensions.Any(ext => path.EndsWith(ext));
    }

    private async Task<bool> PassBusinessRules(CreateBookProfileRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating business rules for book - Title: {Title}, Category: {Category}, Price: {Price}, Stock: {Stock}",
            request.Title, request.Category, request.Price, request.StockQuantity);

        // Rule 1: Daily book addition limit (max 500 per day)
        var today = DateTime.UtcNow.Date;
        var todayCount = await _context.Books
            .CountAsync(b => b.CreatedAt.Date == today, cancellationToken);

        if (todayCount >= 500)
        {
            _logger.LogWarning("Daily book addition limit exceeded - Count: {Count}, Limit: 500", todayCount);
            return false;
        }

        _logger.LogDebug("Daily limit check passed - Count: {Count}/500", todayCount);

        // Rule 2: Technical books minimum price check ($20.00)
        if (request.Category == BookCategory.Technical && request.Price < 20)
        {
            _logger.LogWarning("Technical book price below minimum - Price: {Price}, Minimum: $20.00", request.Price);
            return false;
        }

        // Rule 3: Children's book content restrictions
        if (request.Category == BookCategory.Children)
        {
            var titleLower = request.Title.ToLower();
            var hasRestrictedWords = ChildrenInappropriateWords.Any(word => titleLower.Contains(word));
            if (hasRestrictedWords)
            {
                _logger.LogWarning("Children's book contains restricted content - Title: {Title}", request.Title);
                return false;
            }
        }

        // Rule 4: High-value book stock limit (>$500 = max 10 stock)
        if (request.Price > 500 && request.StockQuantity > 10)
        {
            _logger.LogWarning("High-value book exceeds stock limit - Price: {Price}, Stock: {Stock}, MaxAllowed: 10",
                request.Price, request.StockQuantity);
            return false;
        }

        _logger.LogDebug("All business rules passed for book - Title: {Title}", request.Title);
        return true;
    }

    // Conditional Validation Helper Methods

    private static bool ContainTechnicalKeywords(string title)
    {
        var titleLower = title.ToLower();
        return TechnicalKeywords.Any(keyword => titleLower.Contains(keyword));
    }

    private bool BeAppropriateForChildren(string title)
    {
        var titleLower = title.ToLower();
        return !ChildrenInappropriateWords.Any(word => titleLower.Contains(word));
    }
}

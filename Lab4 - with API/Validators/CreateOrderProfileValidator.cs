using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Lab3.Validators;

/// <summary>
/// Advanced validator for CreateOrderProfileRequest with comprehensive validation rules
/// Includes business logic validation, database checks, and format validation
/// </summary>
public class CreateOrderProfileValidator : AbstractValidator<CreateOrderProfileRequest>
{
    private readonly BookContext _context;
    private readonly ILogger<CreateOrderProfileValidator> _logger;
    
    // Inappropriate content word list for title validation
    private static readonly HashSet<string> InappropriateWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "spam", "scam", "fake", "fraud", "illegal"
    };
    
    // Restricted words for children's books
    private static readonly HashSet<string> RestrictedChildrenWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "violence", "horror", "scary", "adult", "mature"
    };
    
    // Technical keywords for technical book validation
    private static readonly HashSet<string> TechnicalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "programming", "algorithm", "database", "software", "computer", "technical",
        "engineering", "data", "system", "code", "development", "architecture",
        "network", "security", "cloud", "api", "framework", "design", "patterns"
    };
    
    // Valid image extensions
    private static readonly HashSet<string> ValidImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };
    
    // Business rule constants
    private const int MaxDailyOrderLimit = 500;
    private const decimal MinTechnicalPrice = 20.00m;
    private const decimal HighValueThreshold = 500.00m;
    private const int MaxHighValueStock = 10;
    
    // Conditional validation constants
    private const decimal MaxChildrenPrice = 50.00m;
    private const decimal ExpensiveOrderThreshold = 100.00m;
    private const int ExpensiveOrderMaxStock = 20;
    private const int TechnicalBookMaxAgeYears = 5;
    private const int FictionAuthorMinLength = 5;

    public CreateOrderProfileValidator(BookContext context, ILogger<CreateOrderProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        ConfigureTitleValidation();
        ConfigureAuthorValidation();
        ConfigureISBNValidation();
        ConfigureCategoryValidation();
        ConfigurePriceValidation();
        ConfigurePublishedDateValidation();
        ConfigureStockQuantityValidation();
        ConfigureCoverImageUrlValidation();
        ConfigureBusinessRulesValidation();
        ConfigureConditionalValidation();
    }

    private void ConfigureBusinessRulesValidation()
    {
        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
                .WithMessage("Order does not meet business rule requirements");
    }
    
    /// <summary>
    /// Configure conditional validation rules based on order category and cross-field conditions
    /// </summary>
    private void ConfigureConditionalValidation()
    {
        // Technical Order Conditions
        When(x => IsTechnicalCategory(x.Category), () =>
        {
            // Price minimum $20.00 (stricter than base rule)
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(MinTechnicalPrice)
                .WithMessage($"Technical orders must have a minimum price of {MinTechnicalPrice:C}");
            
            // Must contain technical keywords in Title
            RuleFor(x => x.Title)
                .Must(ContainTechnicalKeywords)
                .WithMessage("Technical order title must contain at least one technical keyword (e.g., programming, algorithm, database, software)");
            
            // Must be published within last 5 years (cross-field validation)
            RuleFor(x => x.PublishedDate)
                .Must(BeWithinLastFiveYears)
                .WithMessage("Technical orders must be published within the last 5 years to ensure content relevance");
        });
        
        // Children's Order Conditions
        When(x => IsChildrenCategory(x.Category), () =>
        {
            // Price maximum $50.00
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(MaxChildrenPrice)
                .WithMessage($"Children's orders cannot exceed {MaxChildrenPrice:C}");
            
            // Title must be appropriate for children (no inappropriate words)
            RuleFor(x => x.Title)
                .Must(BeAppropriateForChildren)
                .WithMessage("Children's order title contains words inappropriate for children");
        });
        
        // Fiction Order Conditions
        When(x => IsFictionCategory(x.Category), () =>
        {
            // Author name minimum 5 characters (full name requirement)
            RuleFor(x => x.Author)
                .MinimumLength(FictionAuthorMinLength)
                .WithMessage($"Fiction orders must have author full name (minimum {FictionAuthorMinLength} characters)");
        });
        
        // Cross-Field Validation: Expensive orders (>$100) must have limited stock (≤20 units)
        RuleFor(x => x)
            .Must(x => !IsExpensiveOrder(x.Price) || x.StockQuantity <= ExpensiveOrderMaxStock)
            .WithMessage($"Expensive orders (over {ExpensiveOrderThreshold:C}) must have limited stock (maximum {ExpensiveOrderMaxStock} units)");
    }

    private void ConfigureTitleValidation()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required and cannot be empty")
            .Length(1, 200).WithMessage("Title must be between 1 and 200 characters")
            .Must(NotContainInappropriateContent)
                .WithMessage("Title contains inappropriate content")
            .MustAsync(BeUniqueTitleForAuthor)
                .WithMessage("An order with this title already exists for this author");
    }

    private void ConfigureAuthorValidation()
    {
        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required and cannot be empty")
            .Length(2, 100).WithMessage("Author name must be between 2 and 100 characters")
            .Must(ContainOnlyValidAuthorCharacters)
                .WithMessage("Author name can only contain letters, spaces, hyphens, apostrophes, and dots");
    }

    private void ConfigureISBNValidation()
    {
        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required and cannot be empty")
            .Must(BeValidISBNFormat)
                .WithMessage("ISBN must be a valid format (10 or 13 digits, may contain hyphens)")
            .MustAsync(BeUniqueISBN)
                .WithMessage("An order with this ISBN already exists in the system");
    }

    private void ConfigureCategoryValidation()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Must(BeValidCategory)
                .WithMessage("Category must be one of: Fiction, NonFiction, Technical, Children");
    }

    private void ConfigurePriceValidation()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(10000).WithMessage("Price must be less than $10,000");
    }

    private void ConfigurePublishedDateValidation()
    {
        RuleFor(x => x.PublishedDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Published date cannot be in the future")
            .GreaterThanOrEqualTo(new DateTime(1400, 1, 1))
                .WithMessage("Published date cannot be before year 1400");
    }

    private void ConfigureStockQuantityValidation()
    {
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000 (reasonableness check)");
    }

    private void ConfigureCoverImageUrlValidation()
    {
        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidImageUrl)
                .When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
                .WithMessage("Cover image URL must be a valid HTTP/HTTPS URL")
            .Must(BeValidImageProtocol)
                .When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
                .WithMessage("Cover image URL must use HTTP or HTTPS protocol")
            .Must(EndWithValidImageExtension)
                .When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
                .WithMessage("Cover image URL must end with a valid image extension (.jpg, .jpeg, .png, .gif, .webp)");
    }

    // Title Validation Methods
    private bool NotContainInappropriateContent(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        var words = title.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var containsInappropriate = words.Any(word => InappropriateWords.Contains(word));
        
        if (containsInappropriate)
        {
            _logger.LogWarning("Title contains inappropriate content: {Title}", title);
        }
        
        return !containsInappropriate;
    }

    private async Task<bool> BeUniqueTitleForAuthor(CreateOrderProfileRequest request, string title, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Title == title && o.Author == request.Author, cancellationToken);
            
            if (exists)
            {
                _logger.LogWarning("Duplicate title found for author - Title: {Title}, Author: {Author}", title, request.Author);
            }
            
            return !exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking title uniqueness for author");
            throw;
        }
    }

    // Author Validation Methods
    private bool ContainOnlyValidAuthorCharacters(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            return false;

        // Allow letters (Unicode), spaces, hyphens, apostrophes, and dots
        var pattern = @"^[\p{L}\s\-'.]+$";
        var isValid = Regex.IsMatch(author, pattern);
        
        if (!isValid)
        {
            _logger.LogWarning("Author name contains invalid characters: {Author}", author);
        }
        
        return isValid;
    }

    // ISBN Validation Methods
    private bool BeValidISBNFormat(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        // Remove hyphens for validation
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
        
        // Must be 10 or 13 digits
        if (!Regex.IsMatch(cleanIsbn, @"^\d{10}$|^\d{13}$"))
        {
            _logger.LogWarning("Invalid ISBN format: {ISBN}", isbn);
            return false;
        }

        return true;
    }

    private async Task<bool> BeUniqueISBN(string isbn, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.ISBN == isbn, cancellationToken);
            
            if (exists)
            {
                _logger.LogWarning("Duplicate ISBN found in system: {ISBN}", isbn);
            }
            
            return !exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ISBN uniqueness");
            throw;
        }
    }

    // Category Validation Methods
    private bool BeValidCategory(string category)
    {
        var isValid = Enum.TryParse<OrderCategory>(category, true, out _);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid category value: {Category}", category);
        }
        
        return isValid;
    }

    // Cover Image URL Validation Methods
    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
            
        var isValid = Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid image URL format: {Url}", url);
        }
        
        return isValid;
    }

    private bool BeValidImageProtocol(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            var isValid = uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid protocol for image URL (must be HTTP/HTTPS): {Url}", url);
            }
            
            return isValid;
        }

        return false;
    }

    private bool EndWithValidImageExtension(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        var extension = Path.GetExtension(url)?.ToLowerInvariant();
        var isValid = !string.IsNullOrEmpty(extension) && ValidImageExtensions.Contains(extension);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid image extension for URL: {Url}. Expected: .jpg, .jpeg, .png, .gif, .webp", url);
        }
        
        return isValid;
    }

    // Business Rules Validation Methods
    private async Task<bool> PassBusinessRules(CreateOrderProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting business rules validation for order - Title: {Title}, Category: {Category}, Price: {Price}", 
                request.Title, request.Category, request.Price);

            // Rule 1: Daily order addition limit check (max 500 per day)
            if (!await CheckDailyOrderLimit(cancellationToken))
            {
                _logger.LogWarning("Business Rule Violation: Daily order limit exceeded (max {MaxLimit})", MaxDailyOrderLimit);
                return false;
            }

            // Rule 2: Technical orders minimum price check ($20.00)
            if (!CheckTechnicalOrderMinimumPrice(request))
            {
                _logger.LogWarning("Business Rule Violation: Technical order price ${Price} is below minimum ${MinPrice}", 
                    request.Price, MinTechnicalPrice);
                return false;
            }

            // Rule 3: Children's order content restrictions
            if (!CheckChildrenOrderContentRestrictions(request))
            {
                _logger.LogWarning("Business Rule Violation: Children's order contains restricted content - Title: {Title}", 
                    request.Title);
                return false;
            }

            // Rule 4: High-value order stock limit (>$500 = max 10 stock)
            if (!CheckHighValueOrderStockLimit(request))
            {
                _logger.LogWarning("Business Rule Violation: High-value order (${Price}) exceeds maximum stock limit of {MaxStock} (requested: {RequestedStock})", 
                    request.Price, MaxHighValueStock, request.StockQuantity);
                return false;
            }

            _logger.LogInformation("All business rules passed for order - Title: {Title}", request.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for order");
            throw;
        }
    }

    // Rule 1: Daily order addition limit check
    private async Task<bool> CheckDailyOrderLimit(CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var todayOrderCount = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.CreatedAt.Date == today, cancellationToken);

            var isWithinLimit = todayOrderCount < MaxDailyOrderLimit;
            
            _logger.LogDebug("Daily order limit check: {CurrentCount}/{MaxLimit} orders today, Within limit: {IsWithinLimit}", 
                todayOrderCount, MaxDailyOrderLimit, isWithinLimit);

            return isWithinLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking daily order limit");
            throw;
        }
    }

    // Rule 2: Technical orders minimum price check
    private bool CheckTechnicalOrderMinimumPrice(CreateOrderProfileRequest request)
    {
        if (!Enum.TryParse<OrderCategory>(request.Category, true, out var category))
        {
            return true; // Let category validation handle invalid category
        }

        if (category == OrderCategory.Technical)
        {
            var meetsMinimum = request.Price >= MinTechnicalPrice;
            
            _logger.LogDebug("Technical order price check: ${Price} >= ${MinPrice} = {MeetsMinimum}", 
                request.Price, MinTechnicalPrice, meetsMinimum);
            
            return meetsMinimum;
        }

        return true;
    }

    // Rule 3: Children's order content restrictions
    private bool CheckChildrenOrderContentRestrictions(CreateOrderProfileRequest request)
    {
        if (!Enum.TryParse<OrderCategory>(request.Category, true, out var category))
        {
            return true; // Let category validation handle invalid category
        }

        if (category == OrderCategory.Children)
        {
            var words = request.Title.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var containsRestricted = words.Any(word => RestrictedChildrenWords.Contains(word));
            
            if (containsRestricted)
            {
                _logger.LogWarning("Children's order contains restricted words - Title: {Title}", request.Title);
            }
            
            return !containsRestricted;
        }

        return true;
    }

    // Rule 4: High-value order stock limit
    private bool CheckHighValueOrderStockLimit(CreateOrderProfileRequest request)
    {
        if (request.Price > HighValueThreshold)
        {
            var withinLimit = request.StockQuantity <= MaxHighValueStock;
            
            _logger.LogDebug("High-value order stock check: Price ${Price} > ${Threshold}, Stock {Stock} <= {MaxStock} = {WithinLimit}", 
                request.Price, HighValueThreshold, request.StockQuantity, MaxHighValueStock, withinLimit);
            
            return withinLimit;
        }

        return true;
    }
    
    // ==================== Conditional Validation Helper Methods ====================
    
    /// <summary>
    /// Check if category is Technical
    /// </summary>
    private bool IsTechnicalCategory(string category)
    {
        return Enum.TryParse<OrderCategory>(category, true, out var parsedCategory) 
               && parsedCategory == OrderCategory.Technical;
    }
    
    /// <summary>
    /// Check if category is Children
    /// </summary>
    private bool IsChildrenCategory(string category)
    {
        return Enum.TryParse<OrderCategory>(category, true, out var parsedCategory) 
               && parsedCategory == OrderCategory.Children;
    }
    
    /// <summary>
    /// Check if category is Fiction
    /// </summary>
    private bool IsFictionCategory(string category)
    {
        return Enum.TryParse<OrderCategory>(category, true, out var parsedCategory) 
               && parsedCategory == OrderCategory.Fiction;
    }
    
    /// <summary>
    /// Check if order price exceeds expensive threshold
    /// </summary>
    private bool IsExpensiveOrder(decimal price)
    {
        return price > ExpensiveOrderThreshold;
    }
    
    /// <summary>
    /// ContainTechnicalKeywords(): Check Title against technical keywords list
    /// Technical orders must contain at least one technical keyword in the title
    /// </summary>
    private bool ContainTechnicalKeywords(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Technical order validation: Title is empty");
            return false;
        }

        var words = title.Split(new[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var hasTechnicalKeyword = words.Any(word => TechnicalKeywords.Contains(word));
        
        if (!hasTechnicalKeyword)
        {
            _logger.LogWarning("Technical order validation failed: Title '{Title}' does not contain technical keywords", title);
        }
        else
        {
            _logger.LogDebug("Technical order validation passed: Title '{Title}' contains technical keywords", title);
        }
        
        return hasTechnicalKeyword;
    }
    
    /// <summary>
    /// BeAppropriateForChildren(): Check Title against inappropriate words for children
    /// Children's books must not contain restricted words
    /// </summary>
    private bool BeAppropriateForChildren(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        var words = title.Split(new[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var containsRestrictedWord = words.Any(word => RestrictedChildrenWords.Contains(word));
        
        if (containsRestrictedWord)
        {
            _logger.LogWarning("Children's order validation failed: Title '{Title}' contains inappropriate words", title);
        }
        else
        {
            _logger.LogDebug("Children's order validation passed: Title '{Title}' is appropriate", title);
        }
        
        return !containsRestrictedWord;
    }
    
    /// <summary>
    /// Check if published date is within last 5 years (for technical books)
    /// Technical books must be recent to ensure content relevance
    /// </summary>
    private bool BeWithinLastFiveYears(DateTime publishedDate)
    {
        var fiveYearsAgo = DateTime.UtcNow.AddYears(-TechnicalBookMaxAgeYears);
        var isRecent = publishedDate >= fiveYearsAgo;
        
        if (!isRecent)
        {
            _logger.LogWarning("Technical order validation failed: Published date {PublishedDate} is older than {Years} years", 
                publishedDate, TechnicalBookMaxAgeYears);
        }
        else
        {
            _logger.LogDebug("Technical order validation passed: Published date {PublishedDate} is within last {Years} years", 
                publishedDate, TechnicalBookMaxAgeYears);
        }
        
        return isRecent;
    }
}

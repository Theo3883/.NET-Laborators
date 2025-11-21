using System.ComponentModel.DataAnnotations;

namespace Lab3.Attributes;

/// <summary>
/// Custom validation attribute for price range validation.
/// Accepts min and max price in constructor (as double, converts to decimal).
/// </summary>
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    /// <summary>
    /// Accepts min and max price in constructor (as double, convert to decimal).
    /// Generates error message with currency formatting.
    /// </summary>
    public PriceRangeAttribute(double minPrice, double maxPrice)
    {
        _minPrice = (decimal)minPrice;
        _maxPrice = (decimal)maxPrice;
        
        // Generate error message with currency formatting
        ErrorMessage = $"Price must be between {_minPrice:C} and {_maxPrice:C}";
    }

    /// <summary>
    /// Implements IsValid() method for price range validation.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            // Allow null - use [Required] for required validation
            return ValidationResult.Success;
        }

        // Handle both decimal and convertible numeric types
        decimal price;
        try
        {
            price = Convert.ToDecimal(value);
        }
        catch
        {
            return new ValidationResult(
                "Invalid price format.",
                new[] { validationContext.MemberName ?? "Price" }
            );
        }

        // Validate price is within range
        if (price >= _minPrice && price <= _maxPrice)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage,
            new[] { validationContext.MemberName ?? "Price" }
        );
    }
}

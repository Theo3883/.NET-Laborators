using System.ComponentModel.DataAnnotations;

namespace Lab3.Attributes;

/// <summary>
/// Custom validation attribute for order category validation.
/// Validates that the category is one of the allowed categories.
/// </summary>
public class OrderCategoryAttribute : ValidationAttribute
{
    private readonly string[] _allowedCategories;

    /// <summary>
    /// Accepts allowed categories in constructor.
    /// Generates error message with allowed categories list.
    /// </summary>
    public OrderCategoryAttribute(params string[] allowedCategories)
    {
        _allowedCategories = allowedCategories ?? Array.Empty<string>();
        
        // Generate error message with allowed categories list
        if (_allowedCategories.Length > 0)
        {
            var categoriesList = string.Join(", ", _allowedCategories);
            ErrorMessage = $"Category must be one of: {categoriesList}";
        }
        else
        {
            ErrorMessage = "Invalid category.";
        }
    }

    /// <summary>
    /// Implements IsValid() method to check category against allowed list.
    /// Performs case-insensitive comparison.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            // Allow null/empty - use [Required] for required validation
            return ValidationResult.Success;
        }

        var category = value.ToString()!;

        // Check category against allowed list (case-insensitive)
        var isValid = _allowedCategories.Any(allowed => 
            string.Equals(allowed, category, StringComparison.OrdinalIgnoreCase));

        if (isValid)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage,
            new[] { validationContext.MemberName ?? "Category" }
        );
    }
}

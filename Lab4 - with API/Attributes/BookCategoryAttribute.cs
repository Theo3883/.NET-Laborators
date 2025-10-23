using System.ComponentModel.DataAnnotations;
using Lab3.Model;

namespace Lab3.Attributes;

public class BookCategoryAttribute : ValidationAttribute
{
    private readonly BookCategory[] _allowedCategories;

    public BookCategoryAttribute(params BookCategory[] allowedCategories)
    {
        this._allowedCategories = allowedCategories;
        
        var categoryNames = string.Join(", ", allowedCategories.Select(c => c.ToString()));
        ErrorMessage = $"Category must be one of the following: {categoryNames}";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Category is required.");
        }

        if (value is not BookCategory category)
        {
            return new ValidationResult("Invalid category type.");
        }

        if (_allowedCategories.Contains(category))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}

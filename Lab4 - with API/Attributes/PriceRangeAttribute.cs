using System.ComponentModel.DataAnnotations;

namespace Lab3.Attributes;

public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeAttribute(double minPrice, double maxPrice)
    {
        this._minPrice = (decimal)minPrice;
        this._maxPrice = (decimal)maxPrice;
        
        ErrorMessage = $"Price must be between {this._minPrice:C} and {this._maxPrice:C}";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Price is required.");
        }

        if (value is not decimal price)
        {
            return new ValidationResult("Invalid price type.");
        }

        if (price >= _minPrice && price <= _maxPrice)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}

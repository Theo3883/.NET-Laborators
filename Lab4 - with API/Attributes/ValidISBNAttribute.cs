using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lab3.Attributes;

public class ValidISBNAttribute : ValidationAttribute, IClientModelValidator
{
    public ValidISBNAttribute()
    {
        ErrorMessage = "ISBN must be a valid ISBN-10 or ISBN-13 format.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var isbn = value.ToString()!;
        
        // Remove hyphens and spaces
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Check for ISBN-10 (10 digits, last can be X)
        if (cleanIsbn.Length == 10 && Regex.IsMatch(cleanIsbn, @"^\d{9}[\dX]$"))
        {
            return ValidationResult.Success;
        }

        // Check for ISBN-13 (13 digits starting with 978 or 979)
        if (cleanIsbn.Length == 13 && Regex.IsMatch(cleanIsbn, @"^(978|979)\d{10}$"))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.Add("data-val", "true");
        context.Attributes.Add("data-val-isbn", ErrorMessage ?? "Invalid ISBN format.");
        context.Attributes.Add("data-val-isbn-pattern", @"^[\d\-\s]{10,17}$");
    }
}

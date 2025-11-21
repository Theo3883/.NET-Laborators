using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lab3.Attributes;

/// <summary>
/// Custom validation attribute for ISBN format validation (10 or 13 digits).
/// Supports both client-side and server-side validation.
/// </summary>
public class ValidISBNAttribute : ValidationAttribute, IClientModelValidator
{
    private const string DefaultErrorMessage = "ISBN must be a valid format (10 or 13 digits, may contain hyphens or spaces)";

    public ValidISBNAttribute() : base(DefaultErrorMessage)
    {
    }

    /// <summary>
    /// Implements IsValid() method to validate ISBN format (10 or 13 digits).
    /// Removes hyphens and spaces before validation.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            // Allow null/empty - use [Required] for required validation
            return ValidationResult.Success;
        }

        var isbn = value.ToString()!;
        
        // Remove hyphens and spaces before validation
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Validate 10 or 13 digits
        var isValid = Regex.IsMatch(cleanIsbn, @"^\d{10}$|^\d{13}$");

        if (!isValid)
        {
            return new ValidationResult(
                ErrorMessage ?? DefaultErrorMessage,
                new[] { validationContext.MemberName ?? "ISBN" }
            );
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Implements AddValidation() for client-side validation.
    /// Adds data attributes for client ISBN validation.
    /// </summary>
    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Add data-val attribute to enable client validation
        MergeAttribute(context.Attributes, "data-val", "true");
        
        // Add custom data-val-isbn attribute for error message
        MergeAttribute(context.Attributes, "data-val-isbn", ErrorMessage ?? DefaultErrorMessage);
        
        // Add pattern for client-side regex validation
        MergeAttribute(context.Attributes, "data-val-isbn-pattern", @"^[\d\s\-]{10,17}$");
    }

    /// <summary>
    /// Helper method to merge attributes without duplicates.
    /// </summary>
    private void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}

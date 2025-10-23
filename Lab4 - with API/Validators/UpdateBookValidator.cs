using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Model;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Lab3.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    private readonly BookContext _context;

    public UpdateBookValidator(BookContext context)
    {
        this._context = context;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Book ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(1).WithMessage("Title must be at least 1 character.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MinimumLength(2).WithMessage("Author must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Author name contains invalid characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .Must(BeValidISBN).WithMessage("ISBN must be a valid ISBN-10 or ISBN-13 format.")
            .MustAsync(BeUniqueISBNForUpdate).WithMessage("A book with this ISBN already exists.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid BookCategory value.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThan(10000).WithMessage("Price must be less than $10,000.");

        RuleFor(x => x.PublishedDate)
            .NotEmpty().WithMessage("Published date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future.")
            .GreaterThanOrEqualTo(new DateTime(1400, 1, 1)).WithMessage("Published date cannot be before year 1400.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000.");

        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidImageUrl).When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("Cover image URL must be a valid HTTP/HTTPS image URL.");
    }

    private bool BeValidISBN(string isbn)
    {
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
        if (cleanIsbn.Length == 10)
        {
            return Regex.IsMatch(cleanIsbn, @"^\d{9}[\dX]$");
        }
        if (cleanIsbn.Length == 13)
        {
            return Regex.IsMatch(cleanIsbn, @"^(978|979)\d{10}$");
        }
        return false;
    }
    private async Task<bool> BeUniqueISBNForUpdate(UpdateBookRequest request, string isbn, CancellationToken cancellationToken)
    {
        var exists = await _context.Books
            .AnyAsync(b => b.ISBN == isbn && b.Id != request.Id, cancellationToken);
        return !exists;
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            return false;

        if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            return false;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var path = uriResult.AbsolutePath.ToLower();
        return validExtensions.Any(ext => path.EndsWith(ext));
    }
}

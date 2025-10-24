using FluentValidation;
using Lab3.DTO.Request;
using Lab3.Model;

namespace Lab3.Validators;

public class UpdateOrderValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Order ID is required")
            .Must(BeValidGuid).WithMessage("Order ID must be a valid GUID");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required")
            .Matches(@"^(?:\d{10}|\d{13})$").WithMessage("ISBN must be 10 or 13 digits");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Must(BeValidCategory).WithMessage("Category must be one of: Fiction, NonFiction, Technical, Children");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(10000).WithMessage("Price must be less than 10000");

        RuleFor(x => x.PublishedDate)
            .NotEmpty().WithMessage("Published date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
            .WithMessage("Cover image URL must be a valid URL");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private bool BeValidCategory(string category)
    {
        return Enum.TryParse<OrderCategory>(category, true, out _);
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

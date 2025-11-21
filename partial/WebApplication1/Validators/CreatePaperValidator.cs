using FluentValidation;
using WebApplication1.DTO.Request;

namespace WebApplication1.Validators;

/// <summary>
/// Validator for CreatePaperRequest with comprehensive validation rules
/// </summary>
public class CreatePaperValidator : AbstractValidator<CreatePaperRequest>
{
    public CreatePaperValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty()
            .WithMessage("Title is required and cannot be empty")
            .MaximumLength(200)
            .WithMessage("Title must be at most 200 characters");

        RuleFor(p => p.Author)
            .NotEmpty()
            .WithMessage("Author is required and cannot be empty")
            .MaximumLength(100)
            .WithMessage("Author name must be at most 100 characters");

        RuleFor(p => p.PublishedOn)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Published date cannot be in the future");
    }
}


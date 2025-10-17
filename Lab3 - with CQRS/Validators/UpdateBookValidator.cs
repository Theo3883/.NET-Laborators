using FluentValidation;
using Lab3.DTO.Request;
 

namespace Lab3.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Book ID must be greater than 0.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Author).NotEmpty().WithMessage("Author is required.");
        RuleFor(x => x.Year)
            .GreaterThan(0).WithMessage("Year must be greater than 0.")
            .LessThanOrEqualTo(DateTime.Now.Year).WithMessage("Year cannot be in the future.");
    }
}

using FluentValidation;
using Lab3.DTO.Request;

namespace Lab3.Validators;

public class DeleteBookValidator : AbstractValidator<DeleteBookRequest>
{
    public DeleteBookValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Book ID must be greater than 0.");
    }
}

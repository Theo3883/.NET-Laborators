using FluentValidation;
using Lab3.DTO.Request;

namespace Lab3.Validators;

public class DeleteBookValidator : AbstractValidator<DeleteBookRequest>
{
    public DeleteBookValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Book ID is required.");
    }
}

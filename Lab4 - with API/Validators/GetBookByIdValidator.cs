using FluentValidation;
using Lab3.DTO.Request;

namespace Lab3.Validators;

public class GetBookByIdValidator : AbstractValidator<GetBookByIdRequest>
{
    public GetBookByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Book ID is required.");
    }
}

using FluentValidation;
using Lab3.DTO.Request;

namespace Lab3.Validators;

public class GetOrderByIdValidator : AbstractValidator<GetOrderByIdRequest>
{
    public GetOrderByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Order ID is required")
            .Must(BeValidGuid).WithMessage("Order ID must be a valid GUID");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}

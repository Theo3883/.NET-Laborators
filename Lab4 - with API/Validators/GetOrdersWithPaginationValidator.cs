using FluentValidation;
using Lab3.DTO.Request;

namespace Lab3.Validators;

public class GetOrdersWithPaginationValidator : AbstractValidator<GetOrdersWithPaginationRequest>
{
    public GetOrdersWithPaginationValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");
    }
}

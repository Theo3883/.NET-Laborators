using FluentValidation;
using Lab3.DTO.Request;
 

namespace Lab3.Validators;

public class GetBooksWithPaginationValidator : AbstractValidator<GetBooksWithPaginationRequest>
{
    public GetBooksWithPaginationValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");
        
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100 items per page.");
    }
}

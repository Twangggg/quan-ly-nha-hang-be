using FluentValidation;
using FoodHub.Application.Extensions.Pagination;

namespace FoodHub.Application.Validators
{
    public class PaginationParamsValidator : AbstractValidator<PaginationParams>
    {
        public PaginationParamsValidator()
        {
            RuleFor(x => x.PageIndex).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(5, 50);
            RuleFor(x => x.IsDescending).Must(x => x == true || x == false);
        }
    }
}

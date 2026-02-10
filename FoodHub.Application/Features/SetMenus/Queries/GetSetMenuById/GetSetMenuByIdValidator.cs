using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdValidator : AbstractValidator<GetSetMenuByIdQuery>
    {
        public GetSetMenuByIdValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");
        }
    }
}

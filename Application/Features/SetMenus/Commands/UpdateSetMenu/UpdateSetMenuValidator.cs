using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuValidator : AbstractValidator<UpdateSetMenuCommand>
    {
        public UpdateSetMenuValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["SetMenu.NameRequired"]);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage(localizer["SetMenu.PriceInvalid"]);

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage(localizer["SetMenu.ItemsRequired"]);
        }
    }
}

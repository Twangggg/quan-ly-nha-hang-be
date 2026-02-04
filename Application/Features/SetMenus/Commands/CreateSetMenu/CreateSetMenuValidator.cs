using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuValidator : AbstractValidator<CreateSetMenuCommand>
    {
        public CreateSetMenuValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(localizer["SetMenu.CodeRequired"]);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["SetMenu.NameRequired"]);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage(localizer["SetMenu.PriceInvalid"]);

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage(localizer["SetMenu.ItemsRequired"]);
        }
    }
}

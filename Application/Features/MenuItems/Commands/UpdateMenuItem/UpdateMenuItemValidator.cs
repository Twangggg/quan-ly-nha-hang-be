using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage(localizer["MenuItem.MenuItemIdRequired"]);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(localizer["MenuItem.CodeRequired"])
                .MaximumLength(50).WithMessage(localizer["MenuItem.CodeTooLong"]);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["MenuItem.NameRequired"])
                .MaximumLength(150).WithMessage(localizer["MenuItem.NameTooLong"]);

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage(localizer["MenuItem.ImageRequired"]);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(localizer["MenuItem.CategoryRequired"]);

            RuleFor(x => x.PriceDineIn)
                .GreaterThan(0).WithMessage(localizer["MenuItem.PriceInvalid"]);

            RuleFor(x => x.PriceTakeAway)
                .GreaterThan(0).When(x => x.PriceTakeAway.HasValue).WithMessage(localizer["MenuItem.PriceInvalid"]);

            RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Cost.HasValue);

            RuleFor(x => x.ExpectedTime)
                .GreaterThan(0).WithMessage(localizer["MenuItem.ExpectedTimeInvalid"]);

            RuleFor(x => x.Station)
                .IsInEnum().WithMessage(localizer["MenuItem.StationInvalid"]);
        }
    }
}

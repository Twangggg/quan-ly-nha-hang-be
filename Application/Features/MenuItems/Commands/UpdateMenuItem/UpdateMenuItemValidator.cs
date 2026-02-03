using FluentValidation;
using FoodHub.Domain.Constants;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("ID món ăn không được để trống");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(Messages.MenuItemCodeRequired)
                .MaximumLength(50).WithMessage(Messages.MenuItemCodeTooLong);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(Messages.MenuItemNameRequired)
                .MaximumLength(150).WithMessage(Messages.MenuItemNameTooLong);

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage(Messages.MenuItemImageRequired)
                .MaximumLength(255);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(Messages.MenuItemCategoryRequired);

            RuleFor(x => x.PriceDineIn)
                .GreaterThan(0).WithMessage(Messages.MenuItemPriceInvalid);

            RuleFor(x => x.PriceTakeAway)
                .GreaterThan(0).WithMessage(Messages.MenuItemPriceInvalid)
                .When(x => x.PriceTakeAway.HasValue);

            RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Cost.HasValue);

            RuleFor(x => x.ExpectedTime)
                .GreaterThan(0).WithMessage(Messages.MenuItemExpectedTimeInvalid);

            RuleFor(x => x.Station)
                .IsInEnum().WithMessage("Station không hợp lệ");
        }
    }
}

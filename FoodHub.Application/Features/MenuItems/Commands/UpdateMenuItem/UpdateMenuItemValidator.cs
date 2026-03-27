using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;
using FoodHub.Domain.Enums;


namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator(IMessageService messageService)
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.MenuItem.IdRequired));

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NameRequired))
                .MaximumLength(200).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NameMaxLength));

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Common.ImageUrlMaxLength));

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.MenuItem.CategoryIdRequired));

            RuleFor(x => x.Station)
                .Must(x => Enum.IsDefined(typeof(Station), x)).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.InvalidStation));

            RuleFor(x => x.ExpectedTime)
                .NotEmpty()
                .GreaterThan(0)
                .WithMessage(messageService.GetMessage(MessageKeys.MenuItem.ExpectedTimeMin));

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage(messageService.GetMessage(MessageKeys.MenuItem.PriceMin));
        }
    }
}

using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;
using FoodHub.Domain.Enums;


namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NameRequired))
                .MaximumLength(150).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NameMaxLength));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Common.DescriptionMaxLength));

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.MenuItem.CategoryIdRequired));

            RuleFor(x => x.Station)
                .Must(x => Enum.IsDefined(typeof(Station), x)).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.InvalidStation));

            RuleFor(x => x.ExpectedTime)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.ExpectedTimeMin));

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.PriceMin));

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0).When(x => x.CostPrice.HasValue).WithMessage(messageService.GetMessage(MessageKeys.MenuItem.CostPriceMin));
        }
    }
}

using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuValidator : AbstractValidator<CreateSetMenuCommand>
    {
        public CreateSetMenuValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.NameRequired))
                .MaximumLength(150).WithMessage(messageService.GetMessage(MessageKeys.SetMenu.NameMaxLength));

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.CategoryIdRequired));

            RuleFor(x => x.ImageUrl)
                .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.ImageUrl))
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ImageUrlMaxLength));

            RuleFor(x => x.Description)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage(messageService.GetMessage(MessageKeys.Common.DescriptionMaxLength));

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Common.PriceGreaterZero));

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0).WithMessage(messageService.GetMessage(MessageKeys.Common.CostPriceGreaterEqualZero));

            RuleFor(x => x.Items)
                .NotNull().WithMessage(messageService.GetMessage(MessageKeys.Common.AtLeastOneRequired))
                .Must(items => items != null && items.Any()).WithMessage(messageService.GetMessage(MessageKeys.SetMenu.ItemsRequired));

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity));
            });
        }
    }
}

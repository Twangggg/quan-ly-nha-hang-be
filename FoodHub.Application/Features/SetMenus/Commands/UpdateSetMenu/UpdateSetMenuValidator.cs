using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuValidator : AbstractValidator<UpdateSetMenuCommand>
    {
        public UpdateSetMenuValidator(IMessageService messageService)
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.IdRequired));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Common.DescriptionMaxLength));

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.CategoryIdRequired));

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.NameRequired))
                .MaximumLength(150).WithMessage(messageService.GetMessage(MessageKeys.SetMenu.NameMaxLength));

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Common.PriceGreaterZero));

            RuleFor(x => x.Items)
                .NotNull().WithMessage(messageService.GetMessage(MessageKeys.Common.AtLeastOneRequired))
                .Must(items => items != null && items.Any()).WithMessage(messageService.GetMessage(MessageKeys.SetMenu.ItemsRequired));

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity));
            });

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.MenuItemId).Distinct().Count() == items.Count)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.DuplicateItems));
        }
    }
}

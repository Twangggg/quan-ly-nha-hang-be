using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenCommandValidator : AbstractValidator<SubmitOrderToKitchenCommand>
    {
        public SubmitOrderToKitchenCommandValidator( IMessageService messageService)
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity));
            RuleFor(x => x.TableId)
                .NotEmpty()
                .When(x => x.OrderType == Domain.Enums.OrderType.DineIn)
                .WithMessage(messageService.GetMessage(MessageKeys.Order.SelectTable));
            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.MenuItemId)
                        .NotEmpty()
                        .WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NotFound));
                    item.RuleFor(i => i.Quantity)
                        .GreaterThan(0)
                        .WithMessage(messageService.GetMessage(MessageKeys.MenuItem.InvalodQuantity));
                });
        }
    }
}

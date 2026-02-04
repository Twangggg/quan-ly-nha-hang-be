using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemCommandValidator(IMessageService message)
        {
            RuleFor(o => o.OrderId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.Order.NotFound));
            RuleFor(o => o.MenuItemId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.MenuItem.NotFound));
            RuleFor(o => o.Quantity).GreaterThan(0).WithMessage(message.GetMessage(MessageKeys.Order.InvalidQuantiry));
        }
    }
}

using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemValidator(IMessageService message)
        {
            RuleFor(o => o.OrderId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.Order.NotFound));
            RuleFor(o => o.MenuItemId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.MenuItem.NotFound));
            RuleFor(o => o.Quantity).GreaterThan(0).WithMessage(message.GetMessage(MessageKeys.Order.InvalidQuantity));
        }
    }
}

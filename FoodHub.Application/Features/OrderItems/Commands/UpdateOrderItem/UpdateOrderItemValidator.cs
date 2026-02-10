using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemValidator : AbstractValidator<UpdateOrderItemCommand>
    {
        public UpdateOrderItemValidator()
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.Items).NotEmpty().WithMessage(MessageKeys.Order.InvalidQuantity);
            RuleForEach(o => o.Items).SetValidator(new UpdateOrderItemDtoValidator());
        }
    }

    public class UpdateOrderItemDtoValidator : AbstractValidator<UpdateOrderItemDto>
    {
        public UpdateOrderItemDtoValidator()
        {
            RuleFor(x => x.MenuItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(MessageKeys.Order.InvalidQuantity);
        }
    }
}

using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemValidator : AbstractValidator<UpdateOrderItemCommand>
    {
        public UpdateOrderItemValidator(IMessageService messageService)
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.Items).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Order.InvalidQuantity));
            RuleForEach(o => o.Items).SetValidator(new UpdateOrderItemDtoValidator(messageService));
        }
    }

    public class UpdateOrderItemDtoValidator : AbstractValidator<UpdateOrderItemDto>
    {
        public UpdateOrderItemDtoValidator(IMessageService messageService)
        {
            RuleFor(x => x.MenuItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Order.InvalidQuantity));
        }
    }
}

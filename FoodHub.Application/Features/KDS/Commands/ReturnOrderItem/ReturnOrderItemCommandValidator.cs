using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.KDS.Commands.ReturnOrderItem
{
    public class ReturnOrderItemCommandValidator : AbstractValidator<ReturnOrderItemCommand>
    {
        public ReturnOrderItemCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}

using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.KDS.Commands.RejectOrderItem
{
    public class RejectOrderItemCommandValidator : AbstractValidator<RejectOrderItemCommand>
    {
        public RejectOrderItemCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId).NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
            RuleFor(x => x.Reason).NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OrderItem.RejectionReasonRequired));
        }
    }
}

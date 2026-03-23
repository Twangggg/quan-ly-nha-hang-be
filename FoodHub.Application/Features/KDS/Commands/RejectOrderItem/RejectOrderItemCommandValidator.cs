using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

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

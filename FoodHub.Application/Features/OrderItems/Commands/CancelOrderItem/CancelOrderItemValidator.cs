using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem
{
    public class CancelOrderItemValidator : AbstractValidator<CancelOrderItemCommand>
    {
        public CancelOrderItemValidator(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId).NotEmpty();

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WhenAsync(async (command, cancellation) =>
                {
                    var order = await unitOfWork.Repository<Domain.Entities.OrderItem>()
                        .GetByIdAsync(command.OrderItemId);
                    return order != null && order.Status == OrderItemStatus.Preparing;
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Order.ReasonRequired));
        }
    }
}

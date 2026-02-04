using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderCommandValidator(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            RuleFor(x => x.OrderId).NotEmpty();

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WhenAsync(async (command, cancellation) =>
                {
                    var order = await unitOfWork.Repository<Domain.Entities.Order>()
                        .GetByIdAsync(command.OrderId);
                    return order != null && order.Status == OrderStatus.Preparing;
                })
                .WithMessage(messageService.GetMessage(MessageKeys.ResetPassword.ReasonRequired));
        }
    }
}

using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Orders.Commands.CreateDraftOrder
{
    public class CreateDraftOrderCommandValidatior : AbstractValidator<CreateDraftOrderCommand>
    {
        public CreateDraftOrderCommandValidatior(IMessageService messageService)
        {
            RuleFor(v => v.OrderType)
                .IsInEnum().WithMessage(messageService.GetMessage(MessageKeys.Order.InvalidType));
            RuleFor(v => v.TableId).NotEmpty().When(v => v.OrderType == OrderType.DineIn)
                .WithMessage(messageService.GetMessage(MessageKeys.Order.SelectTable));
        }
    }
}

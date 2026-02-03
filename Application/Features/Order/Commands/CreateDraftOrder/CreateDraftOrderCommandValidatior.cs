using FluentValidation;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Order.Commands.CreateDraftOrder
{
    public class CreateDraftOrderCommandValidatior : AbstractValidator<CreateDraftOrderCommand>
    {
        public CreateDraftOrderCommandValidatior()
        {
            RuleFor(v => v.OrderType)
                .IsInEnum().WithMessage("Invalid Order Type.");
            RuleFor(v => v.TableId).NotEmpty().When(v => v.OrderType == OrderType.DineIn)
                .WithMessage("Please select a table for dine-in orders.");
        }
    }
}

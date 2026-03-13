using AutoMapper;
using FluentValidation;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public class ChangeOrderTableValidator : AbstractValidator<ChangeOrderTableCommand>
    {
        public ChangeOrderTableValidator() {
            RuleFor(r => r.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");
            RuleFor(r => r.TableId)
                .NotEmpty().WithMessage("Table ID is required.");
        }
    }
}

using FluentValidation;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public class SplitOrderValidator : AbstractValidator<SplitOrderCommand>
    {
        public SplitOrderValidator()
        {
            RuleFor(x => x.SourceOrderId)
                .NotEmpty().WithMessage("Source order ID is required.");

            RuleFor(x => x.ItemsToSplit)
                .NotEmpty().WithMessage("At least one item must be selected for splitting.");

            RuleFor(x => x)
                .Must(x => x.DestinationOrderId.HasValue || x.DestinationTableId.HasValue)
                .WithMessage("Destination order or destination table is required.");

            RuleFor(x => x)
                .Must(x => !(x.DestinationOrderId.HasValue && x.DestinationTableId.HasValue))
                .WithMessage("Provide either destination order or destination table, not both.");

            RuleForEach(x => x.ItemsToSplit)
                .ChildRules(items =>
                {
                    items.RuleFor(i => i.OrderItemId)
                        .NotEmpty().WithMessage("Order item ID is required for all items.");
                    items.RuleFor(i => i.QuantityToSplit)
                        .GreaterThan(0).WithMessage("Quantity to split must be greater than zero for all items.");
                });
        }
    }
}

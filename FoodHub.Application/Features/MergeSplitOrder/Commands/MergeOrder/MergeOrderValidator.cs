using FluentValidation;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderValidator : AbstractValidator<MergeOrderCommand>
    {
        public MergeOrderValidator()
        {
            RuleFor(o => o.FirstOrder).NotEmpty();
            RuleFor(o => o.SecondOrder).NotEmpty();
        }
    }
}

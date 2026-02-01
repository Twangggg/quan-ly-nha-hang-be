using FluentValidation;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemValidator : AbstractValidator<UpdateOptionItemCommand>
    {
        public UpdateOptionItemValidator()
        {
            RuleFor(x => x.OptionItemId)
                .NotEmpty().WithMessage("Option item ID is required");

            RuleFor(x => x.Label)
                .NotEmpty().WithMessage("Label is required")
                .MaximumLength(100).WithMessage("Label must not exceed 100 characters");

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Extra price must be greater than or equal to 0");
        }
    }
}

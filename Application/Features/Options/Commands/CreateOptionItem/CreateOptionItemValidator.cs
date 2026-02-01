using FluentValidation;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemValidator : AbstractValidator<CreateOptionItemCommand>
    {
        public CreateOptionItemValidator()
        {
            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage("Option group ID is required");

            RuleFor(x => x.Label)
                .NotEmpty().WithMessage("Label is required")
                .MaximumLength(100).WithMessage("Label must not exceed 100 characters");

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Extra price must be greater than or equal to 0");
        }
    }
}

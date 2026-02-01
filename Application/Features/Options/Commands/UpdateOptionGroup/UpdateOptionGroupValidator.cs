using FluentValidation;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupValidator : AbstractValidator<UpdateOptionGroupCommand>
    {
        public UpdateOptionGroupValidator()
        {
            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage("Option group ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Option group name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid option group type");
        }
    }
}

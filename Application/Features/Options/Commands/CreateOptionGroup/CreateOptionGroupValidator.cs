using FluentValidation;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupValidator : AbstractValidator<CreateOptionGroupCommand>
    {
        public CreateOptionGroupValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("Menu item ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Option group name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid option group type");
        }
    }
}

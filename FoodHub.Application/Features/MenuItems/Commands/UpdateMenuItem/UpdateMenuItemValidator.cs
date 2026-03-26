using FluentValidation;
using FoodHub.Domain.Enums;


namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("Menu item id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Menu item name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.Station)
                .Must(x => Enum.IsDefined(typeof(Station), x)).WithMessage("Invalid station.");

            RuleFor(x => x.ExpectedTime)
            .NotEmpty()
                .GreaterThan(0)
                .WithMessage("Expected time must be greater than 0 minutes.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0.");
        }
    }
}

using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("Menu item id is required.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Menu item code is required.")
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.")
                .Must(code => code.Trim() == code)
                .WithMessage("Code must not contain leading or trailing spaces.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Menu item name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Image is required.")
                .MaximumLength(500);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.Station)
                .IsInEnum().WithMessage("Invalid station.");

            RuleFor(x => x.ExpectedTime)
            .NotEmpty()
                .GreaterThan(0)
                .WithMessage("Expected time must be greater than 0 minutes.");

            RuleFor(x => x.PriceDineIn)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Dine-in price cannot be negative.");

            RuleFor(x => x.PriceTakeAway)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Take-away price cannot be negative.");

            RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Cost.HasValue)
                .WithMessage("Cost cannot be negative.");

            RuleFor(x => x)
                .Must(x => x.PriceDineIn > 0 || x.PriceTakeAway > 0)
                .WithMessage("At least one selling price must be greater than 0.");
        }
    }
}

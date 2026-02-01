using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Menu item code is required")
                .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Menu item name is required")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Image URL is required")
                .MaximumLength(255).WithMessage("Image URL must not exceed 255 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required");

            RuleFor(x => x.Station)
                .IsInEnum().WithMessage("Invalid station");

            RuleFor(x => x.PriceDineIn)
                .GreaterThan(0).WithMessage("Dine-in price must be greater than 0");

            RuleFor(x => x.PriceTakeAway)
                .GreaterThan(0).WithMessage("Take-away price must be greater than 0")
                .When(x => x.PriceTakeAway.HasValue);

            RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0).WithMessage("Cost must be greater than or equal to 0")
                .When(x => x.Cost.HasValue);
        }
    }
}

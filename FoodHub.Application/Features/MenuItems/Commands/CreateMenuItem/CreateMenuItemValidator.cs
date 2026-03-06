using FluentValidation;
using FoodHub.Domain.Enums;


namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

            //RuleFor(x => x.ImageUrl)
            //    .NotEmpty()
            //    .When(x => x.ImageFile == null)
            //    .WithMessage("Either Image URL or Image File is required.")
            //    .MaximumLength(255)
            //    .WithMessage("Image URL must not exceed 255 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category ID is required.");

            RuleFor(x => x.Station)
                .Must(x => Enum.IsDefined(typeof(Station), x)).WithMessage("Invalid station.");

            RuleFor(x => x.ExpectedTime)
                .GreaterThan(0).WithMessage("Expected time must be greater than 0.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

            RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue).WithMessage("Cost must be greater than or equal to 0.");
        }
    }
}

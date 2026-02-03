using FluentValidation;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public record CreateSetMenuCommand(
        string Code,
        string Name,
        decimal Price,
        List<CreateSetMenuItemRequest> Items
    ) : IRequest<Result<CreateSetMenuResponse>>;

    public record CreateSetMenuItemRequest(Guid MenuItemId, int Quantity);

    public class CreateSetMenuValidator : AbstractValidator<CreateSetMenuCommand>
    {
        public CreateSetMenuValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Items list cannot be null.")
                .Must(items => items != null && items.Any()).WithMessage("At least one menu item is required.");

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            });
        }
    }
}

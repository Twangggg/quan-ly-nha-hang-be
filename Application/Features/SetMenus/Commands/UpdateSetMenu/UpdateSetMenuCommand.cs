using FluentValidation;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public record UpdateSetMenuCommand(
        Guid SetMenuId,
        string Name,
        decimal Price,
        List<SetMenuItemRequest> Items
    ) : IRequest<Result<UpdateSetMenuResponse>>;

    public record SetMenuItemRequest(Guid MenuItemId, int Quantity);

    public class UpdateSetMenuValidator : AbstractValidator<UpdateSetMenuCommand>
    {
        public UpdateSetMenuValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");

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


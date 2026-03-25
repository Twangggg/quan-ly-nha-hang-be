using FluentValidation;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.UpdateInventoryGroup
{
    public sealed class UpdateInventoryGroupValidator : AbstractValidator<UpdateInventoryGroupCommand>
    {
        public UpdateInventoryGroupValidator()
        {
            RuleFor(x => x.InventoryGroupId).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.LowStockThreshold)
                .GreaterThanOrEqualTo(0)
                .When(x => x.LowStockThreshold.HasValue);

            RuleFor(x => x.ExpiryWarningDays)
                .GreaterThanOrEqualTo(1)
                .When(x => x.ExpiryWarningDays.HasValue);
        }
    }
}

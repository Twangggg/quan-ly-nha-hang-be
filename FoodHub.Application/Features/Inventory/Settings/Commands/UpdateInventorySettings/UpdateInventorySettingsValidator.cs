using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings
{
    public class UpdateInventorySettingsValidator
        : AbstractValidator<UpdateInventorySettingsCommand>
    {
        public UpdateInventorySettingsValidator(IMessageService messageService)
        {
            RuleFor(x => x.ExpiryWarningDays)
                .GreaterThanOrEqualTo(1)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.InventorySettings.ExpiryWarningDaysMin)
                );

            RuleFor(x => x.DefaultLowStockThreshold)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    messageService.GetMessage(
                        MessageKeys.InventorySettings.DefaultLowStockThresholdMin
                    )
                );

            RuleFor(x => x.AutoDeductOnCompleted)
                .NotNull()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ValidationFailed));

            RuleFor(x => x.MaxCostRecalcDays)
                .InclusiveBetween(1, 365)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.InventorySettings.MaxCostRecalcDaysRange)
                );
        }
    }
}

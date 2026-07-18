using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings
{
    public class UpdateBrandingSettingsValidator : AbstractValidator<UpdateBrandingSettingsCommand>
    {
        public UpdateBrandingSettingsValidator(IMessageService messageService)
        {
            RuleFor(x => x.Phone)
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneInvalid))
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email không hợp lệ.")
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.OperatingDays)
                .MaximumLength(100);

            RuleFor(x => x.OperatingHours)
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(2000);
        }
    }
}

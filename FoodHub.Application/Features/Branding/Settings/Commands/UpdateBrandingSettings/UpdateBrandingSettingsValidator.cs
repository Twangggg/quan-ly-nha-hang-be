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
        }
    }
}

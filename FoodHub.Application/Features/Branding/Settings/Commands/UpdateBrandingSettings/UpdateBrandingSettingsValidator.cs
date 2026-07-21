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

            RuleFor(x => x.Hotline)
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneInvalid))
                .When(x => !string.IsNullOrWhiteSpace(x.Hotline));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Website)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Invalid website URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.Website));

            RuleFor(x => x.GoogleMapUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Invalid Google Map URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleMapUrl));

            RuleFor(x => x.Facebook)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Invalid Facebook URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.Facebook));

            RuleFor(x => x.TaxCode)
                .Matches(@"^\d{10}(\d{3})?$")
                .WithMessage("Tax Code must be 10 or 13 digits.")
                .When(x => !string.IsNullOrWhiteSpace(x.TaxCode));

            RuleFor(x => x.RestaurantCode)
                .NotEmpty().WithMessage("Restaurant Code is required.")
                .MaximumLength(50).WithMessage("Max length is 50.")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Restaurant Code can only contain letters, numbers, and underscores.");

            RuleFor(x => x.PostalCode)
                .Matches(@"^\d+$")
                .WithMessage("Postal Code must contain only numbers.")
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

            RuleFor(x => x.VatPercentage)
                .InclusiveBetween(0, 100)
                .WithMessage("VAT must be between 0 and 100.");

            RuleFor(x => x.ClosingTime)
                .Must((model, closingTime) =>
                {
                    if (string.IsNullOrWhiteSpace(model.OpeningTime) || string.IsNullOrWhiteSpace(closingTime))
                        return true;

                    if (TimeSpan.TryParse(model.OpeningTime, out var open) && TimeSpan.TryParse(closingTime, out var close))
                    {
                        return close > open;
                    }
                    return true;
                })
                .WithMessage("Closing Time must be after Opening Time.")
                .When(x => !string.IsNullOrWhiteSpace(x.ClosingTime));
        }
    }
}

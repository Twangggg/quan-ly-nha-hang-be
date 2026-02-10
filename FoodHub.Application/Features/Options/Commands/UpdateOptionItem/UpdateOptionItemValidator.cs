using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemValidator : AbstractValidator<UpdateOptionItemCommand>
    {
        public UpdateOptionItemValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.Label)
                .NotEmpty().WithMessage(localizer["OptionItem.LabelRequired"]);

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0).WithMessage(localizer["OptionItem.ExtraPriceInvalid"]);
        }
    }
}

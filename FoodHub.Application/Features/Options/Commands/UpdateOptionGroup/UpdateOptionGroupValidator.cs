using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupValidator : AbstractValidator<UpdateOptionGroupCommand>
    {
        public UpdateOptionGroupValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.OptionGroupId).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["OptionGroup.NameRequired"])
                .MaximumLength(100).WithMessage(localizer["OptionGroup.NameRequired"]);
        }
    }
}

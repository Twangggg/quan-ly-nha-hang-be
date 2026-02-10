using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["Category.NameRequired"]);

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage(localizer["Category.TypeInvalid"]);
        }
    }
}

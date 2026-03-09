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
                .NotEmpty().WithMessage(localizer["Category.NameRequired"])
                .MaximumLength(100).WithMessage(localizer["Category.NameMaxLength"]);

            RuleFor(x => x.CodePrefix)
                .NotEmpty().WithMessage(localizer["Category.CodePrefixRequired"])
                .MaximumLength(10).WithMessage(localizer["Category.CodePrefixMaxLength"]);

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage(localizer["Category.TypeInvalid"]);
        }
    }
}

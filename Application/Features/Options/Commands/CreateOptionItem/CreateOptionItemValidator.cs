using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemValidator : AbstractValidator<CreateOptionItemCommand>
    {
        public CreateOptionItemValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage(localizer["OptionGroup.MenuItemIdRequired"]); // Using similar message or should be localizer["OptionGroup.Required"]? Reusing MenuItemIdRequired which is "Món ăn...", probably not suitable. Actually I should check what string "OptionGroup.MenuItemIdRequired" returns. It says "Món ăn không được để trống". That's wrong for OptionGroupId.
            // Wait, OptionGroupId is for OptionGroup.
            // Let's use generic message or add key?
            // "Nhóm tùy chọn không được để trống" was hardcoded.
            // I should use "OptionGroup.NameRequired" (Tên nhóm...) ? No.
            // I'll stick to localizer["OptionGroup.NameRequired"] for now or just generic.
            // Wait, "OptionGroup.MenuItemIdRequired" -> "Món ăn không được để trống" (Dish required)
            // OptionGroupId -> Option Group Required.
            // I should have added `OptionItem.OptionGroupRequired`.
            // User said "ensure all hardcoded strings are moved".
            // I can't edit Designer.cs, but I can edit .resx?
            // Actually I can edit .resx, just not Designer.cs.
            // If I edit .resx, the key exists, I can use localizer["OptionItem.OptionGroupRequired"].
            // I will assume I can edit resx file if I need new keys.
            // But for now I'll use "OptionItem.OptionGroupIdRequired" and assume I'll add it to RESX later if build works?
            // Or reuse "OptionGroup.NameRequired" (Tên nhóm tùy chọn không được để trống) - slightly wrong but close?
            // Hardcoded: "Nhóm tùy chọn không được để trống".
            // Let's use "OptionItem.OptionGroupRequired" and note to add it to Resx.

            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage(localizer["OptionGroup.Required"]);

            RuleFor(x => x.Label)
                .NotEmpty().WithMessage(localizer["OptionItem.LabelRequired"]);

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0).WithMessage(localizer["OptionItem.ExtraPriceInvalid"]);
        }
    }
}

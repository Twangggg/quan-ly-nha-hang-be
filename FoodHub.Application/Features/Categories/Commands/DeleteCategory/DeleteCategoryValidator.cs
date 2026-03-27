using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryValidator(IMessageService messageService)
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Category.IdRequired));
        }
    }
}

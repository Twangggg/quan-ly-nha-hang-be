using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Areas.Commands.UpdateArea
{
    public class UpdateAreaValidator : AbstractValidator<UpdateAreaCommand>
    {
        public UpdateAreaValidator(IMessageService messageService)
        {
            RuleFor(v => v.AreaId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Area.NameRequired))
                .MaximumLength(100).WithMessage("Maximum length of Name is 100 characters");

            RuleFor(v => v.CodePrefix)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Area.CodeRequired))
                .MaximumLength(3).WithMessage("Maximum length of Code is 3 characters");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Maximum length of Description is 500 characters");

            RuleFor(v => v.Type)
                .IsInEnum().WithMessage("Invalid area type");
        }
    }
}

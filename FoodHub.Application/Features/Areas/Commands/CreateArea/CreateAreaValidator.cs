using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Areas.Commands.CreateArea
{
    public class CreateAreaValidator : AbstractValidator<CreateAreaCommand>
    {
        public CreateAreaValidator(IMessageService messageService)
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Area.NameRequired))
                .MaximumLength(100).WithMessage("Maximum length of Name is 100 characters");

            RuleFor(v => v.CodePrefix)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Area.CodeRequired))
                .MaximumLength(10).WithMessage("Maximum length of Code is 10 characters");

            RuleFor(v => v.Type)
                .IsInEnum().WithMessage("Invalid Area Type");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Maximum length of Description is 500 characters");
        }
    }
}

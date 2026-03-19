using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupValidator : AbstractValidator<UpdateOptionGroupCommand>
    {
        public UpdateOptionGroupValidator(IMessageService messageService)
        {
            RuleFor(x => x.OptionGroupId).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OptionGroup.NameRequired))
                .MaximumLength(100)
                .WithMessage(messageService.GetMessage(MessageKeys.OptionGroup.NameRequired));
        }
    }
}

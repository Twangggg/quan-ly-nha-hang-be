using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

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
                .MaximumLength(100).WithMessage(messageService.GetMessage(MessageKeys.Area.NameMaxLength));

            RuleFor(v => v.CodePrefix)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Area.CodeRequired))
                .MaximumLength(10).WithMessage(messageService.GetMessage(MessageKeys.Area.CodeMaxLength));

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Area.DescriptionMaxLength));

            RuleFor(v => v.Type)
                .IsInEnum().WithMessage(messageService.GetMessage(MessageKeys.Area.TypeInvalid));
        }
    }
}

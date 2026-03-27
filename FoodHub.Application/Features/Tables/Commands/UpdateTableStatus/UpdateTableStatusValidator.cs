using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTableStatus
{
    public class UpdateTableStatusValidator : AbstractValidator<UpdateTableStatusCommand>
    {
        public UpdateTableStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Table.IdRequired));
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidStatus));
        }
    }
}

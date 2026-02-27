using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Areas.Commands.DeactivateArea
{
    public class DeactivateAreaValidator : AbstractValidator<DeactivateAreaCommand>
    {
        public DeactivateAreaValidator(IMessageService messageService)
        {
            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}

using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

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

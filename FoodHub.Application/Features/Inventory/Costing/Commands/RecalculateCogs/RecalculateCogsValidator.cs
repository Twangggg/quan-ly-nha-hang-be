using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs
{
    public class RecalculateCogsValidator : AbstractValidator<RecalculateCogsCommand>
    {
        public RecalculateCogsValidator(IMessageService messageService)
        {
            RuleFor(x => x.FromDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidDate));

            RuleFor(x => x.ToDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidDate));

            RuleFor(x => x)
                .Must(x => x.ToDate >= x.FromDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));
        }
    }
}

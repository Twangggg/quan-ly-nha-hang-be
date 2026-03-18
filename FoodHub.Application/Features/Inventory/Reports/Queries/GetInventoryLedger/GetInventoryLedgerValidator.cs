using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public class GetInventoryLedgerValidator : AbstractValidator<GetInventoryLedgerQuery>
    {
        public GetInventoryLedgerValidator(IMessageService messageService)
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty()
                .WithMessage(
                    messageService.GetMessage(MessageKeys.InventoryCheck.IngredientIdRequired)
                );

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));
        }
    }
}

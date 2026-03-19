using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public class GetInventoryLedgerValidator : AbstractValidator<GetInventoryLedgerQuery>
    {
        private const int MaxDateRangeDays = 365;

        public GetInventoryLedgerValidator(IMessageService messageService)
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));

            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage($"Date range cannot exceed {MaxDateRangeDays} days.");
        }

        private static bool HaveValidDateRange(GetInventoryLedgerQuery query)
        {
            return query.ToDate.DayNumber - query.FromDate.DayNumber <= MaxDateRangeDays;
        }
    }
}

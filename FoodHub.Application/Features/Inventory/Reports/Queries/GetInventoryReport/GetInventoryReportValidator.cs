using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public class GetInventoryReportValidator : AbstractValidator<GetInventoryReportQuery>
    {
        private const int MaxDateRangeDays = 365;

        public GetInventoryReportValidator(IMessageService messageService)
        {
            RuleFor(x => x.Pagination.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberAtLeastOne));

            RuleFor(x => x.Pagination.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeBetween, 1, 100));

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));

            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage(
                    $"Date range cannot exceed {MaxDateRangeDays} days."
                );
        }

        private static bool HaveValidDateRange(GetInventoryReportQuery query)
        {
            return query.ToDate.DayNumber - query.FromDate.DayNumber <= MaxDateRangeDays;
        }
    }
}

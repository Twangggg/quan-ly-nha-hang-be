using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public class GetInventoryReportValidator : AbstractValidator<GetInventoryReportQuery>
    {
        public GetInventoryReportValidator(IMessageService messageService)
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));
        }
    }
}

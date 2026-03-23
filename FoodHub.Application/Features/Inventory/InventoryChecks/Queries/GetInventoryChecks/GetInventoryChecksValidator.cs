using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks
{
    public class GetInventoryChecksValidator : AbstractValidator<GetInventoryChecksQuery>
    {
        public GetInventoryChecksValidator(IMessageService messageService)
        {
            RuleFor(x => x.Pagination.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberAtLeastOne));

            RuleFor(x => x.Pagination.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeBetween, 1, 100));

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));
        }
    }
}

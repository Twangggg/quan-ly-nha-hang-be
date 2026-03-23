using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Lots.Queries.GetInventoryLots
{
    public class GetInventoryLotsValidator : AbstractValidator<GetInventoryLotsQuery>
    {
        public GetInventoryLotsValidator(IMessageService messageService)
        {
            RuleFor(x => x.Pagination.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberAtLeastOne));

            RuleFor(x => x.Pagination.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeBetween, 1, 100));

            RuleFor(x => x.Pagination.Search)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Pagination.Search));
        }
    }
}

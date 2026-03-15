using FluentValidation;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public class GetStockInReceiptsQueryValidator : AbstractValidator<GetStockInReceiptsQuery>
    {
        public GetStockInReceiptsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
        }
    }
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Billing.Queries.GetBillingHistory
{
    public class GetBillingHistoryQuery : IRequest<Result<PagedResult<GetBillingHistoryResponse>>>
    {
        public PaginationParams Pagination { get; set; } = null!;
    }
}

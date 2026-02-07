using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.Orders.Queries.GetOrders
{
    public class GetOrdersQuery : IRequest<Result<PagedResult<GetOrdersResponse>>>
    {
        public PaginationParams Pagination { get; set; }
    }
}

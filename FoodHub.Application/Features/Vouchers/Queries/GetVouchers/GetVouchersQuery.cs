using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Queries.GetVouchers
{
    public record GetVouchersQuery(PaginationParams Pagination) : IRequest<Result<PagedResult<GetVouchersResponse>>>;
}

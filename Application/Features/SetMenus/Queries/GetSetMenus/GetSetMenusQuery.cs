using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public record GetSetMenusQuery(
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<Result<PagedResult<GetSetMenusResponse>>>;
}

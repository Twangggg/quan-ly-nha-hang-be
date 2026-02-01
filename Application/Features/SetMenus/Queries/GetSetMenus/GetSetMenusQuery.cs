using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public record GetSetMenusQuery(
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<Result<PagedResult<SetMenuDto>>>;
}

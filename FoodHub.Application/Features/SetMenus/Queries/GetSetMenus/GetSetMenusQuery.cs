using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusQuery : IRequest<Result<PagedResult<GetSetMenusResponse>>>
    {
        public PaginationParams Pagination { get; set; } = null!;
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusHandler : IRequestHandler<GetSetMenusQuery, Result<PagedResult<SetMenuDto>>>
    {
        public GetSetMenusHandler()
        {
        }

        public async Task<Result<PagedResult<SetMenuDto>>> Handle(GetSetMenusQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

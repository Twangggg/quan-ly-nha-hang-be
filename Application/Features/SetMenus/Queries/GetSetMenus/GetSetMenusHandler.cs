using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusHandler : IRequestHandler<GetSetMenusQuery, Result<PagedResult<GetSetMenusResponse>>>
    {
        public GetSetMenusHandler()
        {
        }

        public async Task<Result<PagedResult<GetSetMenusResponse>>> Handle(GetSetMenusQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}


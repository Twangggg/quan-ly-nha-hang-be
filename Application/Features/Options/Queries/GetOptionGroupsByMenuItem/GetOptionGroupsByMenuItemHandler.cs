using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<GetOptionGroupsResponse>>>
    {
        public GetOptionGroupsByMenuItemHandler()
        {
        }

        public async Task<Result<List<GetOptionGroupsResponse>>> Handle(GetOptionGroupsByMenuItemQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

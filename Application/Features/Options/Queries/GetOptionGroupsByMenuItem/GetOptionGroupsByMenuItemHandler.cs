using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<OptionGroupDto>>>
    {
        public GetOptionGroupsByMenuItemHandler()
        {
        }

        public async Task<Result<List<OptionGroupDto>>> Handle(GetOptionGroupsByMenuItemQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

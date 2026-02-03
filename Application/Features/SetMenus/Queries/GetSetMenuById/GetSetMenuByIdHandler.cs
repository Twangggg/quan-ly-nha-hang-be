using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdHandler : IRequestHandler<GetSetMenuByIdQuery, Result<SetMenuDetailDto>>
    {
        public GetSetMenuByIdHandler()
        {
        }

        public async Task<Result<SetMenuDetailDto>> Handle(GetSetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

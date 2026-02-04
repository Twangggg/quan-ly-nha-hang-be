using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdHandler : IRequestHandler<GetSetMenuByIdQuery, Result<GetSetMenuByIdResponse>>
    {
        public GetSetMenuByIdHandler()
        {
        }

        public async Task<Result<GetSetMenuByIdResponse>> Handle(GetSetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}


using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<GetCategoryByIdResponse>>
    {
        public GetCategoryByIdHandler()
        {
        }

        public async Task<Result<GetCategoryByIdResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

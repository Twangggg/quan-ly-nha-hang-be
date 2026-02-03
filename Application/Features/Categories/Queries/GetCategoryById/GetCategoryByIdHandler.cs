using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
    {
        public GetCategoryByIdHandler()
        {
        }

        public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

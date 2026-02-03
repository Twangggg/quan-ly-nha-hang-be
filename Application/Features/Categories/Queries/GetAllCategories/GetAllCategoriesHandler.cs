using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
    {
        public GetAllCategoriesHandler()
        {
        }

        public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

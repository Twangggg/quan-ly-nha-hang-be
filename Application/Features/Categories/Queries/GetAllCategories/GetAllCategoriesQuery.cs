using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public record GetAllCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;
}

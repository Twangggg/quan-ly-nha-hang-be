using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<Result<CategoryDto>>;
}

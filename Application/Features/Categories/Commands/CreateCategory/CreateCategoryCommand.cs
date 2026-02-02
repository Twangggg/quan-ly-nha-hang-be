using FoodHub.Application.DTOs.Categories;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public record CreateCategoryCommand(string Name, int Type) : IRequest<Result<CategoryDto>>;
}

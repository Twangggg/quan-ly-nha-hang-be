using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public record CreateCategoryCommand(string Name, int Type) : IRequest<Result<CategoryDto>>;
}

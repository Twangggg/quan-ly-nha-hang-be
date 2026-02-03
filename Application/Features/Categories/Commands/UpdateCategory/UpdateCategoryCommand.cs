using FoodHub.Application.DTOs.Categories;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public record UpdateCategoryCommand(Guid CategoryId, string Name, int Type) : IRequest<Result<CategoryDto>>;
}

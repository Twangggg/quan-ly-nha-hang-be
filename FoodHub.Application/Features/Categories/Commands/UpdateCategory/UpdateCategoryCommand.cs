using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public record UpdateCategoryCommand(Guid CategoryId, string Name, CategoryType Type) : IRequest<Result<UpdateCategoryResponse>>;
}

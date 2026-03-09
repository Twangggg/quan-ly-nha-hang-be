using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public record CreateCategoryCommand(string Name, string CodePrefix, CategoryType Type) : IRequest<Result<CreateCategoryResponse>>;
}

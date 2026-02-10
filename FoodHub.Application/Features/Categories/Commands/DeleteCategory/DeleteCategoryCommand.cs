using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.DeleteCategory
{
    public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest<Result<DeleteCategoryResponse>>;
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategoryStatus
{
    public record UpdateCategoryStatusCommand(Guid CategoryId, bool IsActive) : IRequest<Result<bool>>;
}

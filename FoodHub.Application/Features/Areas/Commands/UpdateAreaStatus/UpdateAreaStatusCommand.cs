using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus
{
    public record UpdateAreaStatusCommand(Guid AreaId, bool IsActive) : IRequest<Result<bool>>;
}

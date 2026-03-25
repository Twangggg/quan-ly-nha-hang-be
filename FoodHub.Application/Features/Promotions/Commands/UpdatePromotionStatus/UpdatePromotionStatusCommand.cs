using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotionStatus
{
    public sealed record UpdatePromotionStatusCommand(Guid PromotionId, bool IsActive)
        : IRequest<Result<bool>>;
}

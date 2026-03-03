using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Areas.Commands.DeactivateArea
{
    public record DeactivateAreaResponse(Guid AreaId, AreaStatus Status, DateTime? UpdatedAt);
}

using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public record UpdateSetMenuStockStatusCommand(Guid SetMenuId, bool IsOutOfStock) : IRequest<Result<bool>>;
}

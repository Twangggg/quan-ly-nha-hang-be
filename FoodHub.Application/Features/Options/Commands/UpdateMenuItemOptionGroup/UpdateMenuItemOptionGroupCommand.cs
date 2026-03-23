using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateMenuItemOptionGroup
{
    public record UpdateMenuItemOptionGroupCommand(
        Guid MenuItemOptionGroupId,
        bool IsRequired,
        int MinSelect,
        int MaxSelect,
        int SortOrder,
        bool IsVisible
    ) : IRequest<Result<UpdateMenuItemOptionGroupResponse>>;
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.AssignOptionGroupToMenuItem
{
    public record AssignOptionGroupToMenuItemCommand(
        Guid MenuItemId,
        Guid OptionGroupId,
        bool IsRequired,
        int MinSelect,
        int MaxSelect,
        int SortOrder = 0,
        bool IsVisible = true
    ) : IRequest<Result<AssignOptionGroupToMenuItemResponse>>;
}

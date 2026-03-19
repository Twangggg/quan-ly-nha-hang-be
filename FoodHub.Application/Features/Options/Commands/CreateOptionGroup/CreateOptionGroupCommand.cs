using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    /// <summary>
    /// Tạo nhóm tùy chọn reusable và có thể gán ngay cho một menu item để tương thích giai đoạn chuyển tiếp.
    /// </summary>
    public record CreateOptionGroupCommand(
        Guid? MenuItemId,
        string Name,
        OptionGroupType Type,
        bool IsRequired,
        int? MinSelect,
        int? MaxSelect,
        int SortOrder = 0,
        bool IsVisible = true
    )
        : IRequest<Result<CreateOptionGroupResponse>>;
}

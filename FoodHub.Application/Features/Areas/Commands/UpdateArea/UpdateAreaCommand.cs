using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.UpdateArea
{
    /// <summary>
    /// Dữ liệu để cập nhật thông tin khu vực.
    /// </summary>
    public record UpdateAreaCommand : IRequest<Result<GetAreaByIdResponse>>
    {
        /// <summary>ID của khu vực cần cập nhật (được lấy từ route parameter).</summary>
        public Guid AreaId { get; init; }

        /// <summary>Tên khu vực mới.</summary>
        public required string Name { get; init; }

        /// <summary>Mô tả về khu vực (không bắt buộc).</summary>
        public string? Description { get; init; }

        /// <summary>Loại khu vực (Normal, VIP, etc.).</summary>
        public required AreaType Type { get; init; }
    }
}

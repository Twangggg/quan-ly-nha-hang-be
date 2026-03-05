using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.CreateArea
{
    /// <summary>
    /// Dữ liệu để tạo khu vực mới.
    /// </summary>
    public record CreateAreaCommand : IRequest<Result<GetAreaByIdResponse>>
    {
        /// <summary>Tên khu vực (VD: Tầng 1, Sân vườn).</summary>
        public required string Name { get; init; }

        /// <summary>Mã tiền tố cho bàn trong khu vực này (VD: T1). Phải là duy nhất trong hệ thống.</summary>
        public required string CodePrefix { get; init; }

        /// <summary>Loại khu vực (Normal, VIP, etc.).</summary>
        public AreaType Type { get; init; }

        /// <summary>Mô tả thêm về khu vực (không bắt buộc).</summary>
        public string? Description { get; init; }
    }
}

using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    /// <summary>
    /// Model trả về chi tiết của một khu vực.
    /// </summary>
    public class GetAllAreasResponse : IMapFrom<Area>
    {
        /// <summary>
        /// Mã định danh duy nhất của khu vực.
        /// </summary>
        public Guid AreaId { get; set; }

        /// <summary>
        /// Tên của khu vực (VD: Tầng 1, Sân vườn).
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Mã tiền tố cho bàn thuộc khu vực này (VD: T1 cho Tầng 1).
        /// </summary>
        public required string CodePrefix { get; set; }

        /// <summary>
        /// Trạng thái hoạt động hiện tại của khu vực.
        /// </summary>
        public AreaStatus Status { get; set; }

        /// <summary>
        /// Phân loại hình thức của khu vực (VD: Indoor, Outdoor).
        /// </summary>
        public AreaType Type { get; set; }

        /// <summary>
        /// Mô tả thêm hoặc chú thích về khu vực.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Thời điểm tạo bản ghi khu vực.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Thời điểm cập nhật bản ghi gần nhất.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Area, GetAllAreasResponse>();
        }
    }
}

using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftById
{
    /// <summary>
    /// Dữ liệu chi tiết về một ca làm việc phục vụ mục đích hiển thị/tra cứu.
    /// </summary>
    public class GetShiftByIdResponse : IMapFrom<Shift>
    {
        /// <summary>ID CA làm việc.</summary>
        public Guid ShiftId { get; set; }

        /// <summary>Tên ca làm việc.</summary>
        public required string Name { get; set; }

        /// <summary>Giờ bắt đầu.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc.</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Trạng thái (Active/Inactive).</summary>
        public ShiftStatus Status { get; set; }

        /// <summary>Thời điểm tạo.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm cập nhật gần nhất.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}

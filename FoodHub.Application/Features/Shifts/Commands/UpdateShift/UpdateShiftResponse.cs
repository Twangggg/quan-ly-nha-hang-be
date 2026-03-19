using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShift
{
    /// <summary>
    /// Thông tin phản hồi khi cập nhật thành công ca làm việc.
    /// </summary>
    public class UpdateShiftResponse
    {
        /// <summary>ID của ca làm việc vừa được cập nhật.</summary>
        public Guid ShiftId { get; set; }

        /// <summary>Tên ca sau khi cập nhật.</summary>
        public required string Name { get; set; }

        /// <summary>Giờ bắt đầu sau cập nhật.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc sau cập nhật.</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Trạng thái hoạt động hiện tại (Active/Inactive).</summary>
        public ShiftStatus Status { get; set; }

        /// <summary>Thời điểm cập nhật bản ghi.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}

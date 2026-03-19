using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    /// <summary>
    /// Thông tin phản hồi khi tạo mới ca làm việc.
    /// </summary>
    public class CreateShiftResponse
    {
        /// <summary>ID của ca làm việc vừa được tạo.</summary>
        public Guid ShiftId { get; set; }

        /// <summary>Tên ca làm việc.</summary>
        public required string Name { get; set; }

        /// <summary>Giờ bắt đầu.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc.</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Trạng thái (Active/Inactive).</summary>
        public ShiftStatus Status { get; set; }

        /// <summary>Thời điểm tạo bản ghi.</summary>
        public DateTime CreatedAt { get; set; }
    }
}

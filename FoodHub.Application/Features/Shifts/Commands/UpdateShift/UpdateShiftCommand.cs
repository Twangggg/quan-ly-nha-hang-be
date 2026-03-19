using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShift
{
    /// <summary>
    /// Dữ liệu để cập nhật thông tin một ca làm việc.
    /// </summary>
    public record UpdateShiftCommand : IRequest<Result<UpdateShiftResponse>>
    {
        /// <summary>ID Định danh của ca làm việc cần cập nhật.</summary>
        public Guid ShiftId { get; init; }

        /// <summary>Tên ca làm việc mới (VD: Ca gãy, Ca 24h).</summary>
        public string Name { get; init; } = null!;

        /// <summary>Giờ bắt đầu mới của ca.</summary>
        public TimeSpan StartTime { get; init; }

        /// <summary>Giờ kết thúc mới của ca. Phải lớn hơn giờ bắt đầu.</summary>
        public TimeSpan EndTime { get; init; }
    }
}

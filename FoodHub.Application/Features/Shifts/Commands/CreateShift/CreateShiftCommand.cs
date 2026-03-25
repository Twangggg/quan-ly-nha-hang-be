using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    /// <summary>
    /// Dữ liệu để tạo một ca làm việc mới trong hệ thống.
    /// </summary>
    public record CreateShiftCommand : IRequest<Result<CreateShiftResponse>>
    {
        /// <summary>Tên ca làm việc (VD: Ca sáng, Ca tối). Tối đa 100 ký tự.</summary>
        public required string Name { get; init; } = null!;

        /// <summary>Thời gian bắt đầu ca (VD: 07:00:00).</summary>
        public TimeSpan StartTime { get; init; }

        /// <summary>Thời gian kết thúc ca (VD: 12:00:00). Phải lớn hơn thời gian bắt đầu.</summary>
        public TimeSpan EndTime { get; init; }
    }
}

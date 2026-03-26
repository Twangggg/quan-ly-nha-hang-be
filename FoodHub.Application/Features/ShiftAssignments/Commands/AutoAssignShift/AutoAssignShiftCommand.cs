using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AutoAssignShift
{
    /// <summary>
    /// Command để tự động gán ca làm việc cho nhân viên trong một khoảng thời gian.
    /// </summary>
    public record AutoAssignShiftCommand : IRequest<Result<List<AssignShiftResponse>>>
    {
        /// <summary>ID nhân viên.</summary>
        public required Guid EmployeeId { get; init; }

        /// <summary>ID ca làm việc.</summary>
        public required Guid ShiftId { get; init; }

        /// <summary>Ngày bắt đầu (định dạng: yyyy-MM-dd).</summary>
        public required DateOnly FromDate { get; init; }

        /// <summary>Ngày kết thúc (định dạng: yyyy-MM-dd).</summary>
        public required DateOnly ToDate { get; init; }

        /// <summary>Ghi chú thêm.</summary>
        public string? Note { get; init; }
    }
}

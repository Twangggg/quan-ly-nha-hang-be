using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift
{
    /// <summary>
    /// Command để gán một ca làm việc cho một nhân viên vào một ngày cụ thể.
    /// </summary>
    public record AssignShiftCommand : IRequest<Result<AssignShiftResponse>>
    {
        /// <summary>ID nhân viên được phân công.</summary>
        public required Guid EmployeeId { get; init; }

        /// <summary>ID ca làm việc được gán.</summary>
        public required Guid ShiftId { get; init; }

        /// <summary>Ngày làm việc (định dạng: yyyy-MM-dd).</summary>
        public required DateOnly AssignedDate { get; init; }

        /// <summary>Ghi chú thêm (không bắt buộc, tối đa 500 ký tự).</summary>
        public string? Note { get; init; }
    }
}

using System.Text.Json.Serialization;
using System.Collections.Generic;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShiftRange
{
    /// <summary>
    /// Command để gán một ca làm việc cho nhân viên theo một khoảng ngày (tuần/tháng).
    /// </summary>
    public record AssignShiftRangeCommand : IRequest<Result<IEnumerable<AssignShiftResponse>>>
    {
        public required Guid EmployeeId { get; init; }
        public required Guid ShiftId { get; init; }
        public required DateOnly FromDate { get; init; }
        public required DateOnly ToDate { get; init; }

        public List<int>? DaysOfWeek { get; init; }
        public string? Note { get; init; }
    }
}

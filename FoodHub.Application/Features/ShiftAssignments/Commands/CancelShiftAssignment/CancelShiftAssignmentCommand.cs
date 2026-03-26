using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.CancelShiftAssignment
{
    /// <summary>
    /// Command để hủy một phân công ca làm việc theo ID.
    /// </summary>
    /// <param name="ShiftAssignmentId">ID của bản ghi phân công cần hủy.</param>
    public record CancelShiftAssignmentCommand(Guid ShiftAssignmentId) : IRequest<Result<bool>>;
}

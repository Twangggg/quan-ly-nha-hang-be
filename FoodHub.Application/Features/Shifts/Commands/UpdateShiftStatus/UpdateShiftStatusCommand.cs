using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus
{
    /// <summary>
    /// Command để thay đổi trạng thái hoạt động của một ca làm việc.
    /// </summary>
    /// <param name="ShiftId">ID của ca làm việc.</param>
    /// <param name="IsActive">Trạng thái mới (true: Hoạt động, false: Dừng hoạt động).</param>
    public record UpdateShiftStatusCommand(Guid ShiftId, bool IsActive) : IRequest<Result<bool>>;
}

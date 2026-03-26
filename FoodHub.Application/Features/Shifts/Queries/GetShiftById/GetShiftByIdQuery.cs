using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftById
{
    /// <summary>
    /// Request để lấy thông tin chi tiết của một ca làm việc theo ID.
    /// </summary>
    /// <param name="ShiftId">Mã định danh của ca làm việc.</param>
    public record GetShiftByIdQuery(Guid ShiftId) : IRequest<Result<GetShiftByIdResponse>>;
}

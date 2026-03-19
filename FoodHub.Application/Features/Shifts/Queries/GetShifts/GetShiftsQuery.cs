using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Queries.GetShifts
{
    /// <summary>
    /// Request để lấy danh sách tất cả các ca làm việc trong hệ thống.
    /// </summary>
    public record GetShiftsQuery : IRequest<Result<List<GetShiftByIdResponse>>>;
}

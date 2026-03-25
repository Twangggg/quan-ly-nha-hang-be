using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Commands.CheckinAttendance
{
    public record CheckinAttendanceCommand : IRequest<Result<CheckinAttendanceResponse>>;
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Commands.CheckoutAttendance
{
    public class CheckoutAttendanceCommand : IRequest<Result<CheckoutAttendanceResponse>>;
}

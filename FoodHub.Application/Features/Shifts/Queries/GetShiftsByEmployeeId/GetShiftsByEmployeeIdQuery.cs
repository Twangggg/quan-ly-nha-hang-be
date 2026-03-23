using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftsByEmployeeId
{
    public record GetShiftsByEmployeeIdQuery : IRequest<Result<List<GetShiftsByEmployeeIdResponse>>>;
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetMyProfile
{
    public record Query(Guid EmployeeId) : IRequest<Result<Response>>;
}

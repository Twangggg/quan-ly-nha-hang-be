using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public record DeleteEmployeeCommand(Guid EmployeeId) : IRequest<Result<DeleteEmployeeResponse>>;
}

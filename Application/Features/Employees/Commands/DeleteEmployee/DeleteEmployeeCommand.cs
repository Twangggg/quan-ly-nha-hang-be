using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public record DeleteEmployeeCommand(Guid EmployeeId) : IRequest<DeleteEmployeeResponse>;
}

using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public record UpdateEmployeeCommand(
        Guid EmployeeId,
        string? Username,
        string FullName,
        string? Phone,
        string? Address,
        DateOnly? DateOfBirth
        ) : IRequest<UpdateEmployeeResponse>;
}

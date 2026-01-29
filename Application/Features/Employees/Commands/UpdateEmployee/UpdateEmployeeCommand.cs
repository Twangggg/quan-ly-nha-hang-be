using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public record UpdateEmployeeCommand(
        Guid EmployeeId,
        string? Username,
        string FullName,
        string? Phone,
        string? Address,
        EmployeeRole Role,
        DateOnly? DateOfBirth
        ) : IRequest<UpdateEmployeeResponse>;
}

using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public record CreateEmployeeCommand(
        string EmployeeCode,
        string FullName,
        string Email,
        EmployeeRole Role,
        EmployeeStatus Status = EmployeeStatus.Active
        ) : IRequest<CreateEmployeeResponse>, IMapFrom<Employee>;
}

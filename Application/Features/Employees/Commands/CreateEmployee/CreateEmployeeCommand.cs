using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public record CreateEmployeeCommand : IRequest<Result<CreateEmployeeResponse>>, IMapFrom<Employee>
    {
        public string EmployeeCode { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public string Email { get; init; } = null!;
        public EmployeeRole Role { get; init; }
        public EmployeeStatus Status { get; init; } = EmployeeStatus.Active;
    }
}

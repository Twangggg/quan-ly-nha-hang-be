using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.ChangeRole
{
    public class ChangeRoleCommand : IRequest<Result<ChangeRoleResponse>>
    {
        public string EmployeeCode { get; set; } = null!;
        public EmployeeRole CurrentRole { get; set; }
        public EmployeeRole NewRole { get; set; }
    }
}

using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.DTOs.Employees
{
    public class EmployeeDto : IMapFrom<Employee>
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public EmployeeRole Role { get; set; }
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    }

    public class UpdateProfileDto : IMapFrom<Employee>
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
}

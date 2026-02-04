using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public record UpdateEmployeeResponse : IMapFrom<Employee>
    {
        public Guid EmployeeId { get; set; }
        public string? Username { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Status { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public class DeleteEmployeeResponse : IMapFrom<Employee>
    {
        public Guid EmployeeId { get; set; }
        public string Role { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

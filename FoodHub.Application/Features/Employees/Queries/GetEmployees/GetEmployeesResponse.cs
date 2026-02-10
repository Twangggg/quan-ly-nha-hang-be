using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployees
{
    public class GetEmployeesResponse : IMapFrom<Employee>
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Employee, GetEmployeesResponse>()
                .ForMember(d => d.Role,
                    opt => opt.MapFrom(s => s.Role.ToString()))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status.ToString()));
        }
    }
}

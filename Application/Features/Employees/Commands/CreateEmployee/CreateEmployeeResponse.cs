using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public record CreateEmployeeResponse : IMapFrom<Employee>
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Employee, CreateEmployeeResponse>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));
        }
    }
}

using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftsByEmployeeId
{
    public class GetSAsByEmployeeIdResponse : IMapFrom<ShiftAssignment>
    {
        public Guid ShiftAssignmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public ShiftResponse Shift { get; set; } = null!;
        public DateOnly AssignedDate { get; set; }
        public string? Note { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<ShiftAssignment, GetSAsByEmployeeIdResponse>()
                .ForMember(dest => dest.Shift, opt => opt.MapFrom(src => src.Shift));
        }
    }

    public class ShiftResponse : IMapFrom<Shift>
    {
        public Guid ShiftId { get; set; }
        public string Name { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public ShiftStatus Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Shift, ShiftResponse>();
        }
    }
}

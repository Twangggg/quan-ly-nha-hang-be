using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Attendances.Commands.CheckoutAttendance
{
    public class CheckoutAttendanceResponse : IMapFrom<Attendance>
    {
        public Guid AttendanceId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftAssignmentId { get; set; } // FK tới ca được gán (có thể null nếu là làm ngoài giờ)
        public DateTime CheckInTime { get; set; }
        public Boolean? isLate { get; set; }
        public Boolean? isEarlyLeave { get; set; }
        public Boolean? isMissCheckOut { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Attendance, CheckoutAttendanceResponse>()
                .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee));
        }
    }
}

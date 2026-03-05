using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public class CreateTableResponse : IMapFrom<Table>
    {
        public Guid TableId { get; set; }
        public required int TableNumber { get; set; }
        public required int Capacity { get; set; }
        public Guid AreaId { get; set; } = Guid.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<Table, CreateTableResponse>()
                .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.ToString()));
        }
    }
}

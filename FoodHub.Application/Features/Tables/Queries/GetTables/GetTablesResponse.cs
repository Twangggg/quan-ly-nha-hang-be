using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    public class GetTablesResponse
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public required string Area { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Table, GetTablesResponse>()
                .ForMember(d => d.Area,
                    opt => opt.MapFrom(s => s.Area.ToString()));
        }
    }
}

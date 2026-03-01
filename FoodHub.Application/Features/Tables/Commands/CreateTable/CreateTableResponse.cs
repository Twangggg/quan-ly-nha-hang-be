using AutoMapper;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public class CreateTableResponse
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public Guid AreaId { get; set; } = Guid.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string AreaCodePrefix { get; set; } = string.Empty;
        public TableStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}

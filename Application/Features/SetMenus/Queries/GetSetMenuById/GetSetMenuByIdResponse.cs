using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdResponse
    {
        public Guid SetMenuId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public SetType SetType { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public Guid? UpdatedByEmployeeId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual List<GetSetMenuItemByIdResponse> Items { get; set; } = new List<GetSetMenuItemByIdResponse>();
    }

    public class GetSetMenuItemByIdResponse
    {
        public Guid SetMenuItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

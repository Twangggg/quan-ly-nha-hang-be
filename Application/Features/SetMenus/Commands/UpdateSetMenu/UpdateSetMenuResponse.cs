using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuResponse
    {
        public Guid SetMenuId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public SetType SetType { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string? UpdatedByEmployeeId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual List<UpdateSetMenuItemResponse> Items { get; set; } = new List<UpdateSetMenuItemResponse>();
    }

    public class UpdateSetMenuItemResponse
    {
        public Guid SetMenuItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

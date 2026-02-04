using System;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuResponse
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuResponse
    {
        public Guid SetMenuId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<UpdateSetMenuItemResponse> Items { get; set; } = new();
    }

    public class UpdateSetMenuItemResponse
    {
        public Guid SetMenuItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
    }
}

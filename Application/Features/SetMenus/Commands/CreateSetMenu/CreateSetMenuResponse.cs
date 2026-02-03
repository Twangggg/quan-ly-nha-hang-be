namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuResponse
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SetMenuItemResponse> Items { get; set; } = new();
    }

    public class SetMenuItemResponse
    {
        public Guid SetMenuItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
    }
}

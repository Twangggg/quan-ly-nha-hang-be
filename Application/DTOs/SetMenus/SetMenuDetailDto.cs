namespace FoodHub.Application.DTOs.SetMenus
{
    public class SetMenuDetailDto
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Items included in the set menu
        public List<SetMenuItemDto>? Items { get; set; }
    }

    public class SetMenuItemDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}

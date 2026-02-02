namespace FoodHub.Application.DTOs.MenuItems
{
    public class MenuItemDto
    {
        public Guid MenuItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Station { get; set; }
        public int? ExpectedTime { get; set; }
        public decimal PriceDineIn { get; set; }
        public decimal? PriceTakeAway { get; set; }
        public decimal? Cost { get; set; } // Only visible to Manager/Cashier
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

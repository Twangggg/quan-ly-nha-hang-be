namespace FoodHub.Application.DTOs.MenuItems
{
    public class MenuItemDetailDto
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
        public decimal? Cost { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Include OptionGroups for detail view
        public List<OptionGroupDto>? OptionGroups { get; set; }
    }

    public class OptionGroupDto
    {
        public Guid OptionGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsRequired { get; set; }
        public List<OptionItemDto>? OptionItems { get; set; }
    }

    public class OptionItemDto
    {
        public Guid OptionItemId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
}

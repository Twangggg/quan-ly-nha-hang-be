namespace FoodHub.Application.DTOs.SetMenus
{
    public class SetMenuDto
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public SetMenuDto() { }

        public SetMenuDto(Guid setMenuId, string code, string name, decimal price, bool isOutOfStock, DateTime createdAt, DateTime updatedAt)
        {
            SetMenuId = setMenuId;
            Code = code;
            Name = name;
            Price = price;
            IsOutOfStock = isOutOfStock;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}

using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Groups.Queries.GetInventoryGroups
{
    public sealed class GetInventoryGroupsResponse
    {
        public Guid InventoryGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? LowStockThreshold { get; set; }
        public int? ExpiryWarningDays { get; set; }
        public InventoryCostMethod? DefaultCostMethod { get; set; }
        public int IngredientCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckById
{
    public class GetInventoryCheckByIdResponse
    {
        public Guid InventoryCheckId { get; set; }
        public DateTime CheckDate { get; set; }
        public InventoryCheckStatus Status { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalItems { get; set; }
        public IReadOnlyList<GetInventoryCheckByIdItemResponse> Items { get; set; } =
            Array.Empty<GetInventoryCheckByIdItemResponse>();
    }

    public class GetInventoryCheckByIdItemResponse
    {
        public Guid InventoryCheckItemId { get; set; }
        public Guid InventoryCheckId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal BookQuantity { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal DifferenceQuantity { get; set; }
        public string? Reason { get; set; }
    }
}

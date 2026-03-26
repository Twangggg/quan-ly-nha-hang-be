using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Lots.Queries.GetInventoryLots
{
    /// <summary>
    /// Thong tin hien thi cua mot lo ton kho trong danh sach quan ly lo.
    /// </summary>
    public class GetInventoryLotsResponse
    {
        public Guid InventoryLotId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string LotCode { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal OriginalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public string Unit { get; set; } = string.Empty;
        public InventoryLotStatus Status { get; set; }
        public int? DaysRemaining { get; set; }
    }
}

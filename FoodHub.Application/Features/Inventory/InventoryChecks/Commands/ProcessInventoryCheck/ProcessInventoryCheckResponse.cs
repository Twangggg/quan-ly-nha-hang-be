using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck
{
    public class ProcessInventoryCheckResponse
    {
        public Guid InventoryCheckId { get; set; }
        public InventoryCheckStatus Status { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid? StockInReceiptId { get; set; }
        public string? StockInReceiptCode { get; set; }
        public Guid? StockOutReceiptId { get; set; }
        public string? StockOutReceiptCode { get; set; }
    }
}

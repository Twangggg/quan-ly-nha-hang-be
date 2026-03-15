namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt
{
    public class ReverseStockInReceiptResponse
    {
        public Guid StockInReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReversedAt { get; set; }
    }
}

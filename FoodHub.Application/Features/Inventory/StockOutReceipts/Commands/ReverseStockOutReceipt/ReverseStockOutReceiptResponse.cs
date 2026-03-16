namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.ReverseStockOutReceipt
{
    public class ReverseStockOutReceiptResponse
    {
        public Guid StockOutReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReversedAt { get; set; }
    }
}

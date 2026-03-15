namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptResponse
    {
        public Guid StockInReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public int TotalLines { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

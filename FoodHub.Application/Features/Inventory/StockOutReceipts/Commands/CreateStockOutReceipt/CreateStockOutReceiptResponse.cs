namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt
{
    public class CreateStockOutReceiptResponse
    {
        public Guid StockOutReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

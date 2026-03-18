namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts
{
    public class GetStockOutReceiptsResponse
    {
        public Guid StockOutReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CreatedByName { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int TotalItems { get; set; }
    }
}

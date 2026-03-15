namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public class GetStockInReceiptsResponse
    {
        public Guid StockInReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public int TotalLines { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CreatedByName { get; set; }
        public string? Note { get; set; }
    }
}

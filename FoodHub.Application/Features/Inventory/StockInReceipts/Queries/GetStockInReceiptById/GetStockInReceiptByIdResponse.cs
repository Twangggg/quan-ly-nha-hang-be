namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceiptById
{
    public class GetStockInReceiptByIdResponse
    {
        public Guid StockInReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string? Note { get; set; }
        public int TotalLines { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CreatedByName { get; set; }
        public IReadOnlyList<GetStockInReceiptByIdItemResponse> Items { get; set; } =
            Array.Empty<GetStockInReceiptByIdItemResponse>();
    }

    public class GetStockInReceiptByIdItemResponse
    {
        public Guid StockInReceiptItemId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal LineAmount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchCode { get; set; }
    }
}

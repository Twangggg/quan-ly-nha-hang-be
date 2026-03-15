namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptItemDto
    {
        public Guid IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchCode { get; set; }
    }
}

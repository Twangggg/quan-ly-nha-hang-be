using System.Text.Json.Serialization;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptItemDto
    {
        public Guid IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string? BaseUnit { get; set; }
        
        [JsonPropertyName("unitCost")]
        public decimal? UnitCost { get; set; }
        
        [JsonPropertyName("unitPrice")]
        public decimal? UnitPrice 
        { 
            get => UnitCost; 
            set => UnitCost = value ?? UnitCost; 
        }
        
        public DateTime? ExpiryDate { get; set; }
        public string? BatchCode { get; set; }
    }
}

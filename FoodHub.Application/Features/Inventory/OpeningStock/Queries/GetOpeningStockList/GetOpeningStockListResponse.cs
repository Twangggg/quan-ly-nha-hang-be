namespace FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList
{
    public class GetOpeningStockListResponse
    {
        public Guid IngredientId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal CostPrice { get; set; }
    }
}

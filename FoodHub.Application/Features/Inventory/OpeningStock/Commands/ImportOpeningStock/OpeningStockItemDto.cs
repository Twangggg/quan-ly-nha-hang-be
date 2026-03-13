namespace FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock
{
    public class OpeningStockItemDto
    {
        public Guid IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? CostPrice { get; set; }

        public decimal? InitialQuantity
        {
            get => null;
            set
            {
                if (value.HasValue)
                {
                    Quantity = value.Value;
                }
            }
        }
    }
}

namespace FoodHub.Application.Features.Inventory.ImportBalance.Queries.ParseInventoryBalanceExcel;

public class ParsedInventoryBalanceResponse
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientCode { get; set; } = string.Empty;
    public string? IngredientName { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public string? Unit { get; set; }
    public int RowNumber { get; set; }
    public bool IsExist { get; set; }
}

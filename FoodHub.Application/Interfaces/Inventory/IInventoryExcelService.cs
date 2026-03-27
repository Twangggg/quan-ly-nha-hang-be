namespace FoodHub.Application.Interfaces.Inventory;

public interface IInventoryExcelService
{
    Task<List<InventoryBalanceImportDto>> ParseExcelFileAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default
    );
}

public class InventoryBalanceImportDto
{
    public string IngredientCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public string? Unit { get; set; }
    public int RowNumber { get; set; }
}

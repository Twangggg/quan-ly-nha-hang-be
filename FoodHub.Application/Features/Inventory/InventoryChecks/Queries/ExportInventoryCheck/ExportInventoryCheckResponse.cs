namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;

public class ExportInventoryCheckResponse
{
    public Guid InventoryCheckId { get; set; }
    public DateTime CheckDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalBookValue { get; set; }
    public decimal TotalPhysicalValue { get; set; }
    public decimal TotalDifferenceValue { get; set; }
    public IReadOnlyList<ExportInventoryCheckItemResponse> Items { get; set; } = Array.Empty<ExportInventoryCheckItemResponse>();
}

public class ExportInventoryCheckItemResponse
{
    public string IngredientCode { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal BookQuantity { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public decimal BookValue { get; set; }
    public decimal PhysicalValue { get; set; }
    public decimal DifferenceValue { get; set; }
    public string? Reason { get; set; }
}
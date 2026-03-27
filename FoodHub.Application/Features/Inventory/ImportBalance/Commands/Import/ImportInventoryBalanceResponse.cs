namespace FoodHub.Application.Features.Inventory.ImportBalance.Commands.Import;

public class ImportInventoryBalanceResponse
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalRows { get; set; }
    public List<ImportInventoryBalanceError> Errors { get; set; } = new();
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

public class ImportInventoryBalanceError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
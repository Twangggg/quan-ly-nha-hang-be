namespace FoodHub.Application.Interfaces.Inventory;

public interface IReceiptCodeGenerator
{
    Task<string> GenerateStockInReceiptCodeAsync(DateTime receivedAt, CancellationToken cancellationToken = default);
    Task<string> GenerateStockOutReceiptCodeAsync(DateTime stockOutDate, CancellationToken cancellationToken = default);
}

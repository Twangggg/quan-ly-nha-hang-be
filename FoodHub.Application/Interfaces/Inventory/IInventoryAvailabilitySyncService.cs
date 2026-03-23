namespace FoodHub.Application.Interfaces.Inventory
{
    public interface IInventoryAvailabilitySyncService
    {
        Task SyncAfterStockChangeAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken
        );
    }
}

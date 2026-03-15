namespace FoodHub.Application.Interfaces
{
    public interface IInventoryAvailabilitySyncService
    {
        Task SyncAfterStockChangeAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken
        );
    }
}

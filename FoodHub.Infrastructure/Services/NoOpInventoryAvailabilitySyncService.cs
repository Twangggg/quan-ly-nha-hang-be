using FoodHub.Application.Interfaces;

namespace FoodHub.Infrastructure.Services
{
    public class NoOpInventoryAvailabilitySyncService : IInventoryAvailabilitySyncService
    {
        public Task SyncAfterStockChangeAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }
}

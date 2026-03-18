using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Infrastructure.Services.Inventory
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

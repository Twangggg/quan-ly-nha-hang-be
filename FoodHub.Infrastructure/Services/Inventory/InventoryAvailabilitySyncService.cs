using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.Services.Inventory
{
    public class InventoryAvailabilitySyncService : IInventoryAvailabilitySyncService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryAvailabilitySyncService> _logger;

        public InventoryAvailabilitySyncService(
            IUnitOfWork unitOfWork,
            ILogger<InventoryAvailabilitySyncService> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task SyncAfterStockChangeAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken
        )
        {
            if (ingredientIds == null || !ingredientIds.Any())
            {
                return;
            }

            _logger.LogInformation(
                "Syncing MenuItem availability for {Count} ingredients",
                ingredientIds.Count
            );

            // Find all MenuItems that use these ingredients
            var affectedMenuItemIds = await _unitOfWork
                .Repository<MenuItemIngredient>()
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .Select(x => x.MenuItemId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!affectedMenuItemIds.Any())
            {
                return;
            }

            // For each MenuItem, check if ANY ingredient is out of stock
            var menuItems = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Include(m => m.Ingredients)
                    .ThenInclude(i => i.Ingredient)
                .Where(m => affectedMenuItemIds.Contains(m.MenuItemId))
                .ToListAsync(cancellationToken);

            foreach (var menuItem in menuItems)
            {
                var isCurrentlyOutOfStock = menuItem.IsOutOfStock;

                // OutOfStock if ANY required ingredient has CurrentStock <= 0
                var shouldBeOutOfStock = menuItem.Ingredients.Any(i =>
                    i.Ingredient != null && i.Ingredient.CurrentStock <= 0
                );

                if (isCurrentlyOutOfStock != shouldBeOutOfStock)
                {
                    _logger.LogInformation(
                        "Updating MenuItem availability: {MenuItemName} ({MenuItemId}) | Old: {Old} | New: {New}",
                        menuItem.Name,
                        menuItem.MenuItemId,
                        isCurrentlyOutOfStock ? "Out" : "In",
                        shouldBeOutOfStock ? "Out" : "In"
                    );

                    menuItem.IsOutOfStock = shouldBeOutOfStock;
                    _unitOfWork.Repository<MenuItem>().Update(menuItem);
                }
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);
        }
    }
}

using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.Services.Inventory
{
    public class InventoryDeductionService : IInventoryDeductionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ILogger<InventoryDeductionService> _logger;

        public InventoryDeductionService(
            IUnitOfWork unitOfWork,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            ILogger<InventoryDeductionService> logger
        )
        {
            _unitOfWork = unitOfWork;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _logger = logger;
        }

        public async Task DeductStockAsync(
            Guid orderId,
            CancellationToken cancellationToken = default
        )
        {
            _logger.LogInformation("Starting stock deduction for OrderId: {OrderId}", orderId);

            var order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for stock deduction", orderId);
                return;
            }

            var orderItems = order
                .OrderItems.Where(oi =>
                    (oi.Status == OrderItemStatus.Completed || oi.Status == OrderItemStatus.Ready)
                    && !oi.StockDeducted
                )
                .ToList();

            if (!orderItems.Any())
            {
                _logger.LogInformation(
                    "No order items to deduct stock for OrderId: {OrderId}",
                    orderId
                );
                return;
            }

            // Create a general StockOutReceipt for the sale
            var receiptCode = $"SALE-{order.OrderCode}";
            var receipt = StockOutReceipt.Create(
                receiptCode,
                DateTime.UtcNow,
                $"Sale for Order {order.OrderCode}",
                order.CreatedBy
            );

            await _unitOfWork.Repository<StockOutReceipt>().AddAsync(receipt);

            foreach (var item in orderItems)
            {
                var recipe = await _unitOfWork
                    .Repository<MenuItemIngredient>()
                    .Query()
                    .Where(ri => ri.MenuItemId == item.MenuItemId)
                    .Include(ri => ri.Ingredient)
                    .ToListAsync(cancellationToken);

                if (!recipe.Any())
                {
                    _logger.LogDebug(
                        "No recipe found for MenuItemId: {MenuItemId} in OrderItem: {OrderItemId}",
                        item.MenuItemId,
                        item.OrderItemId
                    );
                    continue;
                }

                foreach (var ingredientUsage in recipe)
                {
                    var quantityToDeduct = ingredientUsage.QuantityPerServing * item.Quantity;
                    var ingredient = ingredientUsage.Ingredient;

                    _logger.LogInformation(
                        "Deducting {Quantity} {Unit} of {Ingredient} for OrderItem {OrderItemId}",
                        quantityToDeduct,
                        ingredient.BaseUnit,
                        ingredient.Name,
                        item.OrderItemId
                    );

                    var result = ingredient.ReduceStock(quantityToDeduct, order.CreatedBy);
                    if (!result.IsSuccess)
                    {
                        _logger.LogError(
                            "Failed to reduce stock for ingredient {IngredientId}: {Error}",
                            ingredient.IngredientId,
                            result.ErrorCode
                        );
                        // In real production, we might want to handle this differently (e.g., allow negative stock if configured)
                    }

                    // Record Transaction
                    var transaction = InventoryTransaction.CreateSaleDeduction(
                        ingredient.IngredientId,
                        quantityToDeduct,
                        ingredient.CostPrice,
                        ingredient.CurrentStock,
                        $"Order:{order.OrderCode}|Item:{item.OrderItemId}",
                        order.CreatedBy
                    );

                    // We need to fix InventoryTransaction.CreateStockOut to use SaleDeduction type or create a new method
                    // For now, I'll use a hack or update InventoryTransaction if possible.
                    // Actually, I just added SaleDeduction to the enum.
                    // Let's assume we use a private Create or I'll update InventoryTransaction entity later.

                    await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);

                    // Add to receipt
                    receipt.AddItem(
                        ingredient.IngredientId,
                        quantityToDeduct,
                        ingredient.CostPrice,
                        order.CreatedBy
                    );
                }

                // Mark item as deducted to avoid double deduction
                // Since order is AsNoTracking, we need to get the tracked item or update via repository
                var trackedItem = await _unitOfWork
                    .Repository<OrderItem>()
                    .Query()
                    .FirstOrDefaultAsync(
                        oi => oi.OrderItemId == item.OrderItemId,
                        cancellationToken
                    );
                if (trackedItem != null)
                {
                    trackedItem.StockDeducted = true;
                    _unitOfWork.Repository<OrderItem>().Update(trackedItem);
                }
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Collect all unique ingredient IDs that were involved in deduction
            var affectedIngredientIds = new HashSet<Guid>();
            foreach (var item in orderItems)
            {
                var recipeIngredients = await _unitOfWork
                    .Repository<MenuItemIngredient>()
                    .Query()
                    .Where(ri => ri.MenuItemId == item.MenuItemId)
                    .Select(ri => ri.IngredientId)
                    .ToListAsync(cancellationToken);

                foreach (var id in recipeIngredients)
                {
                    affectedIngredientIds.Add(id);
                }
            }

            if (affectedIngredientIds.Any())
            {
                await _inventoryAvailabilitySyncService.SyncAfterStockChangeAsync(
                    affectedIngredientIds.ToList(),
                    cancellationToken
                );
            }

            _logger.LogInformation("Stock deduction completed for OrderId: {OrderId}", orderId);
        }

        public async Task DeductStockForItemAsync(
            Guid orderItemId,
            CancellationToken cancellationToken = default
        )
        {
            _logger.LogInformation(
                "Starting stock deduction for OrderItemId: {OrderItemId}",
                orderItemId
            );

            var orderItem = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken);

            if (orderItem == null)
            {
                _logger.LogWarning(
                    "OrderItem {OrderItemId} not found for stock deduction",
                    orderItemId
                );
                return;
            }

            if (orderItem.StockDeducted)
            {
                _logger.LogInformation(
                    "Stock already deducted for OrderItemId: {OrderItemId}",
                    orderItemId
                );
                return;
            }

            var recipe = await _unitOfWork
                .Repository<MenuItemIngredient>()
                .Query()
                .Where(ri => ri.MenuItemId == orderItem.MenuItemId)
                .Include(ri => ri.Ingredient)
                .ToListAsync(cancellationToken);

            if (!recipe.Any())
            {
                _logger.LogDebug(
                    "No recipe found for MenuItemId: {MenuItemId} in OrderItem: {OrderItemId}",
                    orderItem.MenuItemId,
                    orderItemId
                );
                return;
            }

            var order = orderItem.Order;
            var receiptCode = $"SALE-{order!.OrderCode}-{orderItemId}";
            var receipt = StockOutReceipt.Create(
                receiptCode,
                DateTime.UtcNow,
                $"Sale for Order {order.OrderCode} - Item {orderItemId}",
                order.CreatedBy
            );
            await _unitOfWork.Repository<StockOutReceipt>().AddAsync(receipt);

            var affectedIngredientIds = new List<Guid>();

            foreach (var ingredientUsage in recipe)
            {
                var quantityToDeduct = ingredientUsage.QuantityPerServing * orderItem.Quantity;
                var ingredient = ingredientUsage.Ingredient;

                _logger.LogInformation(
                    "Deducting {Quantity} {Unit} of {Ingredient} for OrderItem {OrderItemId}",
                    quantityToDeduct,
                    ingredient.BaseUnit,
                    ingredient.Name,
                    orderItemId
                );

                var result = ingredient.ReduceStock(quantityToDeduct, order.CreatedBy);
                if (!result.IsSuccess)
                {
                    _logger.LogError(
                        "Failed to reduce stock for ingredient {IngredientId}: {Error}",
                        ingredient.IngredientId,
                        result.ErrorCode
                    );
                }

                var transaction = InventoryTransaction.CreateSaleDeduction(
                    ingredient.IngredientId,
                    quantityToDeduct,
                    ingredient.CostPrice,
                    ingredient.CurrentStock,
                    $"Order:{order.OrderCode}|Item:{orderItemId}",
                    order.CreatedBy
                );

                await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);

                receipt.AddItem(
                    ingredient.IngredientId,
                    quantityToDeduct,
                    ingredient.CostPrice,
                    order.CreatedBy
                );

                affectedIngredientIds.Add(ingredient.IngredientId);
            }

            orderItem.StockDeducted = true;
            _unitOfWork.Repository<OrderItem>().Update(orderItem);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            if (affectedIngredientIds.Any())
            {
                await _inventoryAvailabilitySyncService.SyncAfterStockChangeAsync(
                    affectedIngredientIds,
                    cancellationToken
                );
            }

            _logger.LogInformation(
                "Stock deduction completed for OrderItemId: {OrderItemId}",
                orderItemId
            );
        }
    }
}

using System;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        protected InventoryTransaction() { }

        public Guid InventoryTransactionId { get; private set; }
        public Guid IngredientId { get; private set; }
        public InventoryTransactionType TransactionType { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal? UnitCost { get; private set; }
        public decimal BalanceAfter { get; private set; }
        public string? Reference { get; private set; }
        public DateTime OccurredAt { get; private set; }

        public virtual Ingredient Ingredient { get; private set; } = null!;

        public static InventoryTransaction CreateOpeningStock(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string? reference = null,
            Guid? createdBy = null
        )
        {
            var occurredAt = DateTime.UtcNow;

            return new InventoryTransaction
            {
                InventoryTransactionId = Guid.NewGuid(),
                IngredientId = ingredientId,
                TransactionType = InventoryTransactionType.OpeningStock,
                Quantity = quantity,
                UnitCost = unitCost,
                BalanceAfter = balanceAfter,
                Reference = reference,
                OccurredAt = occurredAt,
                CreatedAt = occurredAt,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public static InventoryTransaction CreateStockIn(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.StockIn,
                quantity,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        public static InventoryTransaction CreateStockInReverse(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.StockInReverse,
                -quantity,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        public static InventoryTransaction CreateStockOut(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.StockOut,
                -quantity,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        public static InventoryTransaction CreateStockOutReverse(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.StockOutReverse,
                quantity,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        public static InventoryTransaction CreateSaleDeduction(
            Guid ingredientId,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.SaleDeduction,
                -quantity,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        public static InventoryTransaction CreateInventoryCheck(
            Guid ingredientId,
            decimal quantityDifference,
            decimal? unitCost,
            decimal balanceAfter,
            string reference,
            Guid? createdBy = null
        )
        {
            return Create(
                ingredientId,
                InventoryTransactionType.InventoryCheck,
                quantityDifference,
                unitCost,
                balanceAfter,
                reference,
                createdBy
            );
        }

        private static InventoryTransaction Create(
            Guid ingredientId,
            InventoryTransactionType transactionType,
            decimal quantity,
            decimal? unitCost,
            decimal balanceAfter,
            string? reference,
            Guid? createdBy
        )
        {
            var occurredAt = DateTime.UtcNow;

            return new InventoryTransaction
            {
                InventoryTransactionId = Guid.NewGuid(),
                IngredientId = ingredientId,
                TransactionType = transactionType,
                Quantity = quantity,
                UnitCost = unitCost,
                BalanceAfter = balanceAfter,
                Reference = reference,
                OccurredAt = occurredAt,
                CreatedAt = occurredAt,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }
    }
}

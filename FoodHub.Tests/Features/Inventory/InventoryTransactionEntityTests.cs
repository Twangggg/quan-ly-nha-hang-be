using FluentAssertions;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Tests.Features.Inventory
{
    public class InventoryTransactionEntityTests
    {
        [Fact]
        public void CreateOpeningStock_Should_CreateExpectedTransaction()
        {
            var ingredientId = Guid.NewGuid();

            var transaction = InventoryTransaction.CreateOpeningStock(
                ingredientId,
                10,
                2.5m,
                10,
                null
            );

            transaction.IngredientId.Should().Be(ingredientId);
            transaction.TransactionType.Should().Be(InventoryTransactionType.OpeningStock);
            transaction.Quantity.Should().Be(10);
            transaction.UnitCost.Should().Be(2.5m);
            transaction.BalanceAfter.Should().Be(10);
        }

        [Fact]
        public void CreateInventoryCheck_Should_CreateExpectedTransaction()
        {
            var ingredientId = Guid.NewGuid();
            var inventoryCheckId = Guid.NewGuid().ToString();

            var transaction = InventoryTransaction.CreateInventoryCheck(
                ingredientId,
                -3,
                2.5m,
                7,
                inventoryCheckId
            );

            transaction.IngredientId.Should().Be(ingredientId);
            transaction.TransactionType.Should().Be(InventoryTransactionType.InventoryCheck);
            transaction.Quantity.Should().Be(-3);
            transaction.UnitCost.Should().Be(2.5m);
            transaction.BalanceAfter.Should().Be(7);
            transaction.Reference.Should().Be(inventoryCheckId);
        }
    }
}

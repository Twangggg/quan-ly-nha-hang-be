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
    }
}

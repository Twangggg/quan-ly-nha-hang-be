using FluentAssertions;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;

namespace FoodHub.Tests.Features.Inventory
{
    public class IngredientStockInTests
    {
        [Fact]
        public void ReceiveStock_Should_RecalculateWeightedAverageCost()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);

            var result = ingredient.ReceiveStock(5, 6);

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(15);
            ingredient.CostPrice.Should().Be(4);
        }

        [Fact]
        public void ReverseReceivedStock_Should_RestorePreviousStockAndCost()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            ingredient.ReceiveStock(5, 6);

            var result = ingredient.ReverseReceivedStock(5, 6);

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(10);
            ingredient.CostPrice.Should().Be(3);
        }

        [Fact]
        public void ReceiveStock_Should_Fail_WhenQuantityIsInvalid()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);

            var result = ingredient.ReceiveStock(0, 5);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(DomainErrors.Ingredient.InvalidStockInQuantity);
        }
    }
}
